using System;
namespace Framework.Core.Logging.Helper
{
    public interface ICorrelationIdHelper
    {
        void Set(string correlationId);
        string Get();
    }
}

