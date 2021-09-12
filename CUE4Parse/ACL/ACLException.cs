using System;

namespace CUE4Parse.ACL
{
    public class ACLException : Exception
    {
        public ACLException(string? message) : base(message) { }
    }
}