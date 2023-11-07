using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Readers;
using Ionic.Zlib;

namespace CUE4Parse.UE4.IO
{
    public class IoChunkToc
    {
        public readonly FOnDemandTocHeader Header;
        public readonly FTocMeta Meta;
        public readonly FOnDemandTocContainerEntry[] Containers;

        public IoChunkToc(string file) : this(new FileInfo(file)) { }
        public IoChunkToc(FileInfo file) : this(new FByteArchive(file.FullName, File.ReadAllBytes(file.FullName))) { }
        private IoChunkToc(FArchive Ar)
        {
            Header = new FOnDemandTocHeader(Ar);

            if (Header.Version >= EOnDemandTocVersion.Meta)
                Meta = new FTocMeta(Ar);

            Containers = Ar.ReadArray(() => new FOnDemandTocContainerEntry(Ar));
        }
    }

    public class IoChunkContainerStream : Stream
	{
		public string FileName { get; }

		private readonly FOnDemandTocEntry[] _entries;
		private readonly HttpClient _client;

		public IoChunkContainerStream(FOnDemandTocContainerEntry tocContainer)
		{
			FileName = $"{tocContainer.ContainerName}.ucas";

			foreach (var entry in tocContainer.Entries)
			{
				Length += (long) entry.RawSize;
			}

            _entries = tocContainer.Entries;
			_client = new HttpClient(new HttpClientHandler
			{
				UseProxy = false,
				UseCookies = false,
				CheckCertificateRevocationList = false,
				UseDefaultCredentials = false,
				AutomaticDecompression = DecompressionMethods.None
			});
		}

		public override bool CanRead => true;
		public override bool CanSeek => true;
		public override bool CanWrite => false;
		public override long Length { get; }

		private long _position;
		public override long Position
		{
			get => _position;
			set
			{
				if (value > Length || value < 0)
				{
					throw new ArgumentOutOfRangeException();
				}

				_position = value;
			}
		}

		// public async Task SaveAsync(Stream destination, CancellationToken cancellationToken = default)
		// {
		// 	if (destination == null || destination == Null || !destination.CanWrite)
		// 	{
		// 		return;
		// 	}
		//
		// 	foreach (var fileChunkPart in _fileChunkParts)
		// 	{
		// 		var chunk = _chunks[fileChunkPart.Guid];
		// 		await using var chunkStream = await GetChunkStreamAsync(chunk, cancellationToken).ConfigureAwait(false);
		// 		chunkStream.Position = fileChunkPart.Offset;
		//
		// 		var buffer = ArrayPool<byte>.Shared.Rent(fileChunkPart.Size);
		//
		// 		await chunkStream.ReadAsync(buffer.AsMemory(0, fileChunkPart.Size), cancellationToken).ConfigureAwait(false);
		// 		await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, fileChunkPart.Size), cancellationToken).ConfigureAwait(false);
		//
		// 		ArrayPool<byte>.Shared.Return(buffer);
		// 	}
		//
		// 	await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
		// }
		//
		// public void Save(Stream destination)
		// {
		// 	SaveAsync(destination, CancellationToken.None).GetAwaiter().GetResult();
		// }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private async Task<Stream> GetChunkStreamAsync(FOnDemandTocEntry chunk, CancellationToken cancellationToken)
		{
			// var cachePath = _chunkCacheDir == null ? null : Path.Combine(_chunkCacheDir, chunk.Filename);
			//
			// if (cachePath != null && File.Exists(cachePath))
			// {
			// 	var fs = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			// 	return fs;
			// }

			using var request = new HttpRequestMessage(HttpMethod.Get, $"https://download.epicgames.com/ias/fortnite/chunks/{chunk.Hash.ToString().ToLower()[..2]}/{chunk.Hash.ToString().ToLower()}.iochunk");
            request.Headers.Add("Authorization", "bearer bccd4eea566e4ae0b11e3c40c850737d");
            using var response = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
			var chunkData = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

			var outStream = new MemoryStream(chunkData, false);

			// if (cachePath == null)
			// {
			// 	return outStream;
			// }

			// {
			// 	await using var cacheFs = new FileStream(cachePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
			// 	await outStream.CopyToAsync(cacheFs, cancellationToken).ConfigureAwait(false);
			// }

			outStream.Position = 0L;
			return outStream;
		}

		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			var bytesRead = await ReadAtAsync(_position, buffer, offset, count, cancellationToken).ConfigureAwait(false);
			_position += bytesRead;
			return bytesRead;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
		}

		public async Task<int> ReadAtAsync(long position, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			var (i, startPos) = GetChunkIndex(position);
			if (i == -1)
			{
				return 0;
			}

			var bytesRead = 0;

			while (true)
			{
				var chunkPart = _entries[i];
                await using var chunkStream = await GetChunkStreamAsync(chunkPart, cancellationToken).ConfigureAwait(false);

				var chunkOffset = chunkPart.BlockOffset + startPos;
				chunkStream.Position = chunkOffset;
				var chunkBytes = (int) chunkPart.EncodedSize - startPos;
				var bytesLeft = count - bytesRead;

				if (bytesLeft <= chunkBytes)
				{
					await chunkStream.ReadAsync(buffer.AsMemory(bytesRead + offset, bytesLeft), cancellationToken).ConfigureAwait(false);
					bytesRead += bytesLeft;
					break;
				}

				await chunkStream.ReadAsync(buffer.AsMemory(bytesRead + offset, chunkBytes), cancellationToken).ConfigureAwait(false);
				bytesRead += chunkBytes;
				startPos = 0;

				if (++i == _entries.Length)
				{
					break;
				}
			}

			return bytesRead;
		}

		public int ReadAt(long position, byte[] buffer, int offset, int count)
		{
			return ReadAtAsync(position, buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			Position = origin switch
			{
				SeekOrigin.Begin => offset,
				SeekOrigin.Current => offset + _position,
				SeekOrigin.End => Length + offset,
				_ => throw new ArgumentOutOfRangeException(nameof(offset), offset, null)
			};
			return _position;
		}

		private (int Index, int ChunkPos) GetChunkIndex(long position)
		{
			for (var i = 0; i < _entries.Length; i++)
			{
				var size = (long) _entries[i].EncodedSize;
				if (position < size)
				{
					return (i, (int)position);
				}

				position -= size;
			}

			return (-1, -1);
		}

		public override void Flush()
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}
	}
}
