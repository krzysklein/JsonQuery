using System;

namespace JsonQuery.Contracts
{
    public class JsonQueryException : Exception
    {
        public JsonQueryException(string message)
            : base(message)
        {
        }
    }
}
