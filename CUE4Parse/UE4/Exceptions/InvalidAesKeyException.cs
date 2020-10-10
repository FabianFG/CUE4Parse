using System;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Exceptions
{
    public class InvalidAesKeyException : ParserException
    {
        public InvalidAesKeyException(string? message = null, Exception? innerException = null) : base(message, innerException) { }
        
        public InvalidAesKeyException(FArchive reader, string? message = null, Exception? innerException = null) : base(reader, message, innerException) { }
    }
}