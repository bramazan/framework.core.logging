using System;
using System.Reflection;

namespace Framework.Core.Logging.Logging.AppLogger
{
    public interface IAppLogger
    {
        string MethodEntry(object logEventInfo, MethodBase methodBase);
        string MethodExit(object logEventInfo, MethodBase methodBase, double methodExecutionDuration, string messageCode);
        string Trace(string message, MethodBase methodBase);
        string Exception(Exception exception, MethodBase methodBase, string extraMessage = "");
        void Log(string messageTemplate, params object?[]? propertyValues);
    }
}

