using System;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Exceptions
{
    [Serializable]
    public class ParserException : Exception
    {
        public ParserException(string? message = null, Exception? innerException = null) : base(message, innerException)
        { }

        public ParserException(FArchive reader, string? message = null, Exception? innerException = null)
            : base($"{message}\n{reader.GetType().Name} Info: {reader.Name} | Pos:{reader.Position} Length:{reader.Length} ({Math.Round((decimal)reader.Position / reader.Length * 100, 1)}% done)", innerException)
        { }
    }
}
