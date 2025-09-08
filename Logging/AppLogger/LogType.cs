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
        HttpClientResponse = 8,
        Database = 9,
        DatabaseError = 10,
        Redis = 11,
        RedisError = 12,
        Queue = 13,
        QueueError = 14,
        BackgroundService = 15,
        Information = 16,
        Warning = 17,
        Error = 18
    }
}

