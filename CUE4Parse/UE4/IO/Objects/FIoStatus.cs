using System;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects
{
    public enum EIoErrorCode
    {
        Ok,
        Unknown,
        InvalidCode,
        Cancelled,
        FileOpenFailed,
        FileNotOpen,
        ReadError,
        WriteError,
        NotFound,
        CorruptToc,
        UnknownChunkID,
        InvalidParameter,
        SignatureError,
        InvalidEncryptionKey,
    }
    
    public class FIoStatus
    {
        public readonly EIoErrorCode ErrorCode;
        public readonly string ErrorMessage;

        public FIoStatus(EIoErrorCode errorCode, string errorMessage)
        {
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        public override string ToString() { return $"{ErrorMessage} ({ErrorCode})"; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FIoStatusException ToException() { return new FIoStatusException(this); }
    }

    public class FIoStatusException : ParserException
    {
        public readonly FIoStatus Status;

        public FIoStatusException(FIoStatus status, Exception? innerException = null) : base(status.ToString(), innerException)
        {
            Status = status;
        }

        public FIoStatusException(EIoErrorCode errorCode, string errorMessage = "", Exception? innerException = null) : 
            this(new FIoStatus(errorCode, errorMessage), innerException) { }

        public FIoStatusException(FArchive Ar, FIoStatus status, Exception? innerException = null) : base(Ar, status.ToString(), innerException)
        {
            Status = status;
        }

        public FIoStatusException(FArchive Ar, EIoErrorCode errorCode, string errorMessage = "", Exception? innerException = null) :
            this(Ar, new FIoStatus(errorCode, errorMessage), innerException) { }
    }
}