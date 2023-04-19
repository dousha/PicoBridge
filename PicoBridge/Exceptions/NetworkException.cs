using System;

namespace PicoBridge.Exceptions;

public class NetworkException : Exception
{
    public NetworkException(string why)
    {
        Message = why;
    }

    public override string Message { get; }
}
