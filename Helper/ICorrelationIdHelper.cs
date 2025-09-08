using System;
namespace Framework.Core.Logging.Helper
{
    /// <summary>
    /// Thread-safe correlation ID management interface
    /// </summary>
    public interface ICorrelationIdHelper
    {
        /// <summary>
        /// Gets the current correlation ID, generates new one if not set
        /// </summary>
        string Get();

        /// <summary>
        /// Sets the correlation ID for current execution context
        /// </summary>
        void Set(string correlationId);

        /// <summary>
        /// Clears the current correlation ID
        /// </summary>
        void Clear();
    }
}

