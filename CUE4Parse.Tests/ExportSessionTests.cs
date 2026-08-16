using System.Runtime.CompilerServices;
using CUE4Parse.Tests.Fixtures.UE5_8;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Exporters;
using CUE4Parse_Conversion.Options;
using static CUE4Parse.Tests.Fixtures.UE5_8.FixtureTestUtilities;

namespace CUE4Parse.Tests;

public class ExportSessionTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RunAsync_ReleasesCompletedExportersWhileOtherWorkIsRunning()
    {
        const int fastCount = 16;
        using var gate = new ManualResetEventSlim();
        var session = new ExportSession { MaxDegreeOfParallelism = 2 };
        session.Add(new TestExporter("Slow", gate: gate));
        var references = QueueExporters(session, fastCount);
        using var directory = new TempDirectory();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var progress = new InlineProgress(value =>
        {
            if (value.Completed == fastCount) completed.TrySetResult();
        });
        var run = Task.Run(() => session.RunAsync(directory.Path, new ExportOptions(), progress, Ct), Ct);
        try
        {
            await completed.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
            await AssertEventually(() => references.Count(reference => reference.IsAlive) <= 1,
                "Completed exporters remained strongly referenced.");
        }
        finally
        {
            gate.Set();
            await run.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        }
    }

    [Fact]
    public async Task RunAsync_ProcessesDependenciesAndDeduplicatesObjectPaths()
    {
        var session = new ExportSession { MaxDegreeOfParallelism = 2 };
        session.Add(new TestExporter("Root", enqueue: current =>
        {
            current.Add(new TestExporter("Dependency"));
            current.Add(new TestExporter("Dependency"));
        }));

        var results = await Run(session);
        Assert.Equal(["Dependency.Dependency", "Root.Root"],
            results.Select(result => result.ObjectPath).Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task Add_ProviderBackedObjectCanBeCollectedAndReloadedForExport()
    {
        using var provider = CreateMountedIoStoreProvider(FixtureSerialization.Tagged,
            compression: FixtureCompression.Uncompressed);
        var session = new ExportSession { MaxDegreeOfParallelism = 1 };
        var texture = QueueTexture(session, provider);
        await AssertEventually(() => !texture.IsAlive, "The queued texture remained strongly referenced.");
        using var directory = new TempDirectory();
        var result = Assert.Single(await session.RunAsync(directory.Path, new ExportOptions(), ct: Ct));
        Assert.True(result.Success, result.Error?.ToString());
        Assert.All(Assert.IsAssignableFrom<IReadOnlyList<string>>(result.DiskFilePaths),
            path => Assert.True(File.Exists(path)));
    }

    [Fact]
    public async Task Remove_ReleasesTheQueuedExporter()
    {
        var session = new ExportSession();
        var (objectPath, exporter) = QueueRemovableExporter(session);
        Assert.True(session.Remove(objectPath));
        Assert.Equal(0, session.TotalQueued);
        await AssertEventually(() => !exporter.IsAlive, "A removed exporter remained strongly referenced.");
    }

    [Fact]
    public async Task RunAsync_CancellationStopsWorkersAndClearsTheSession()
    {
        using var gate = new ManualResetEventSlim();
        using var started = new ManualResetEventSlim();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var session = new ExportSession { MaxDegreeOfParallelism = 2 };
        session.Add(new TestExporter("Blocked", gate: gate, started: started));
        session.Add(new TestExporter("Pending"));
        using var directory = new TempDirectory();
        var run = Task.Run(() => session.RunAsync(directory.Path, new ExportOptions(), ct: cancellation.Token), Ct);
        try
        {
            Assert.True(await Task.Run(() => started.Wait(TimeSpan.FromSeconds(10)), Ct));
            await cancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await run.WaitAsync(TimeSpan.FromSeconds(10), Ct));
            Assert.False(session.IsRunning);
            Assert.False(session.HasQueuedItems);
        }
        finally
        {
            gate.Set();
        }
    }

    [Fact]
    public async Task RunAsync_SessionCanBeReusedAfterCompletion()
    {
        var session = new ExportSession { MaxDegreeOfParallelism = 1 };
        using var directory = new TempDirectory();
        session.Add(new TestExporter("First"));
        var first = Assert.Single(await session.RunAsync(directory.Path, new ExportOptions(), ct: Ct));
        session.Add(new TestExporter("Second"));
        var second = Assert.Single(await session.RunAsync(directory.Path, new ExportOptions(), ct: Ct));
        Assert.Equal(("First.First", "Second.Second"), (first.ObjectPath, second.ObjectPath));
    }

    private static async Task<IReadOnlyList<ExportResult>> Run(ExportSession session)
    {
        using var directory = new TempDirectory();
        return await session.RunAsync(directory.Path, new ExportOptions(), ct: Ct);
    }

    private static async Task AssertEventually(Func<bool> condition, string message)
    {
        for (var attempt = 0; attempt < 40 && !condition(); attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await Task.Delay(25, Ct);
        }
        Assert.True(condition(), message);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static List<WeakReference> QueueExporters(ExportSession session, int count)
    {
        var references = new List<WeakReference>(count);
        for (var i = 0; i < count; i++)
        {
            var exporter = new TestExporter($"Fast{i}", new byte[128 * 1024]);
            references.Add(new WeakReference(exporter));
            session.Add(exporter);
        }
        return references;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference QueueTexture(ExportSession session, CUE4Parse.FileProvider.DefaultFileProvider provider)
    {
        var texture = LoadExport<UTexture2D>(provider,
            "CUE4ParseFixtures/Content/Fixtures/Textures/T_BGRA8.uasset", "T_BGRA8");
        session.Add(texture);
        return new WeakReference(texture);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (string, WeakReference) QueueRemovableExporter(ExportSession session)
    {
        var exporter = new TestExporter("Removable", new byte[128 * 1024]);
        session.Add(exporter);
        return (exporter.ObjectPath, new WeakReference(exporter));
    }

    private sealed class InlineProgress(Action<ExportProgress> report) : IProgress<ExportProgress>
    {
        public void Report(ExportProgress value) => report(value);
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "CUE4Parse.Tests", Guid.NewGuid().ToString("N"));
        public TempDirectory() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }

    private sealed class TestExporter(string name, byte[]? payload = null, ManualResetEventSlim? gate = null,
        ManualResetEventSlim? started = null, Action<ExportSession>? enqueue = null) : ExporterBase(new UObject { Name = name })
    {
        protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default)
        {
            started?.Set();
            gate?.Wait(ct);
            enqueue?.Invoke(Session);
            GC.KeepAlive(payload);
            return [new ExportFile("bin", [1])];
        }
    }
}
