using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CUE4Parse_Conversion.V2;

public sealed class ExportSession(DirectoryInfo baseDirectory, ExporterOptions options)
{
    public DirectoryInfo BaseDirectory { get; } = baseDirectory;
    public ExporterOptions Options { get; } = options;
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;

    private readonly HashSet<IExporter2> _roots = [];
    private readonly ConcurrentQueue<IExporter2> _childQueue = new();
    private readonly ConcurrentDictionary<string, byte> _seenPaths = new(StringComparer.OrdinalIgnoreCase);
    private int _totalQueued;

    public ExportSession(string baseDirectory, ExporterOptions options) : this(new DirectoryInfo(baseDirectory), options)
    {

    }

    public ExportSession Add(ExporterBase2 exporter)
    {
        exporter._session = this;
        _roots.Add(exporter);
        return this;
    }

    public ExportSession AddRange(IEnumerable<ExporterBase2> exporters)
    {
        foreach (var exporter in exporters)
        {
            exporter._session = this;
            _roots.Add(exporter);
        }
        return this;
    }

    public bool TryEnqueue(ExporterBase2 child)
    {
        if (!_seenPaths.TryAdd(child.ObjectPath, 0))
            return false;

        child._session = this;
        _childQueue.Enqueue(child);
        Interlocked.Increment(ref _totalQueued);
        return true;
    }

    public async Task<IReadOnlyList<ExportResult>> RunAsync(IProgress<ExportProgress>? progress = null, CancellationToken ct = default)
    {
        // Pre-register all roots so they can't be re-queued by children.
        foreach (var r in _roots)
            _seenPaths.TryAdd(r.ObjectPath, 0);

        Interlocked.Add(ref _totalQueued, _roots.Count);

        var completed = 0;
        var allResults = new ConcurrentBag<ExportResult>();
        var options = new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism, CancellationToken = ct };

        var current = new List<IExporter2>(_roots);
        while (current.Count > 0)
        {
            await Parallel.ForEachAsync(current, options, Process).ConfigureAwait(false);

            current.Clear();
            while (_childQueue.TryDequeue(out var child))
            {
                current.Add(child);
            }
        }

        return [.. allResults];

        async ValueTask Process(IExporter2 exporter, CancellationToken token)
        {
            IReadOnlyList<ExportResult> results;
            try
            {
                results = await exporter.ExportAsync(progress, token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                results = [ExportResult.Failure(exporter.ObjectName, exporter.PackagePath, exporter.PackageDirectory, ex)];
            }

            foreach (var result in results)
            {
                allResults.Add(result);

                var c = Interlocked.Increment(ref completed);
                var total = Volatile.Read(ref _totalQueued);
                progress?.Report(new ExportProgress(c, total, result));
            }
        }
    }
}
