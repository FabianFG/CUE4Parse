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
            : base($"{message} (Archive {reader.Name} Pos {reader.Position} Length {reader.Length})", innerException)
        { }
    }
}