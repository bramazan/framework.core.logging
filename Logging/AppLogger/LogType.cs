using System;
namespace Framework.Core.Logging.Logging.AppLogger
{
    public enum LogType
    {
        MethodEntry = 1,
        MethodExit = 2,
        Trace = 3,
        Exception = 4,
        HttpRequest = 5,
        HttpResponse = 6,
        HttpClientRequest = 7,
        HttpClientResponse = 8
    }
}

