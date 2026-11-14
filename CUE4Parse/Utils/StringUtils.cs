using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using CUE4Parse.Encryption.Aes;

namespace CUE4Parse.Utils
{
    public static class StringUtils
    {
        private static readonly SearchValues<char> _invalidFileNameChars = SearchValues.Create(Path.GetInvalidFileNameChars());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FAesKey ParseAesKey(this string s)
        {
            return new FAesKey(s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseAesKey(this string s, out FAesKey key)
        {
            try
            {
                key = ParseAesKey(s);
                return true;
            }
            catch (Exception)
            {
                key = null;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringBefore(this string s, char delimiter)
        {
            var index = s.IndexOf(delimiter);
            return index == -1 ? s : s.Substring(0, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringBefore(this string s, string delimiter, StringComparison comparisonType = StringComparison.Ordinal)
        {
            var index = s.IndexOf(delimiter, comparisonType);
            return index == -1 ? s : s.Substring(0, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringAfter(this string s, char delimiter)
        {
            var index = s.IndexOf(delimiter);
            return index == -1 ? s : s.Substring(index + 1, s.Length - index - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringAfter(this string s, string delimiter, StringComparison comparisonType = StringComparison.Ordinal)
        {
            var index = s.IndexOf(delimiter, comparisonType);
            return index == -1 ? s : s.Substring(index + delimiter.Length, s.Length - index - delimiter.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<char> SubstringAfter(this Span<char> s, ReadOnlySpan<char> delimiter)
        {
            var index = s.IndexOf(delimiter);
            return index == -1 ? s : s[(index + delimiter.Length)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringBeforeLast(this string s, char delimiter)
        {
            var index = s.LastIndexOf(delimiter);
            return index == -1 ? s : s.Substring(0, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringBeforeWithLast(this string s, char delimiter)
        {
            var index = s.LastIndexOf(delimiter);
            return index == -1 ? s : s.Substring(0, index + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringBeforeLast(this string s, string delimiter, StringComparison comparisonType = StringComparison.Ordinal)
        {
            var index = s.LastIndexOf(delimiter, comparisonType);
            return index == -1 ? s : s.Substring(0, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringAfterLast(this string s, char delimiter)
        {
            var index = s.LastIndexOf(delimiter);
            return index == -1 ? s : s.Substring(index + 1, s.Length - index - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringAfterWithLast(this string s, char delimiter)
        {
            var index = s.LastIndexOf(delimiter);
            return index == -1 ? s : s.Substring(index, s.Length - index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringAfterLast(this string s, string delimiter, StringComparison comparisonType = StringComparison.Ordinal)
        {
            var index = s.LastIndexOf(delimiter, comparisonType);
            return index == -1 ? s : s.Substring(index + delimiter.Length, s.Length - index - delimiter.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(this string orig, string value, StringComparison comparisonType) => orig.IndexOf(value, comparisonType) >= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string NormalizePath(this string path) => path.TrimStart('/').Replace('\\', '/');

        public static string SanitizePath(this string path)
        {
            Span<char> buffer = path.Length <= 512 ? stackalloc char[path.Length] : new char[path.Length];
            var written = 0;
            var segmentStart = 0;
            for (var i = 0; i <= path.Length; i++)
            {
                if (i < path.Length && path[i] is not ('/' or '\\'))
                {
                    var c = path[i];
                    buffer[written++] = _invalidFileNameChars.Contains(c) ? '_' : c;
                    continue;
                }

                if (written == segmentStart)
                    continue;

                var segment = buffer[segmentStart..written];
                if (segment.SequenceEqual(".") || segment.SequenceEqual(".."))
                {
                    buffer[segmentStart] = '_';
                    written = segmentStart + 1;
                }

                if (i < path.Length)
                    buffer[written++] = '/';

                segmentStart = written;
            }

            if (written > 0 && buffer[written - 1] == '/')
                written--;

            var result = buffer[..written];
            return result.SequenceEqual(path) ? path : new string(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetReadableSize<T>(this T size) where T : INumber<T>
        {
            if (size == T.Zero) return "0 B";

            string[] sizes = ["B", "KB", "MB", "GB", "TB"];
            var order = 0;
            var converted = double.CreateChecked(size);
            while (converted >= 1024 && order < sizes.Length - 1)
            {
                order++;
                converted /= 1024;
            }

            return $"{converted:F2} {sizes[order]}".TrimStart();
        }
    }
}
