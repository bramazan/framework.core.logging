using System;
using System.Threading;

namespace Framework.Core.Logging.Helper
{
    /// <summary>
    /// Thread-safe correlation ID helper using AsyncLocal pattern
    /// Eliminates dependency on IHttpContextAccessor for singleton lifetime compatibility
    /// </summary>
    public class CorrelationIdHelper : ICorrelationIdHelper
    {
        // AsyncLocal provides thread-safe storage that flows with async/await
        private static readonly AsyncLocal<string> _correlationId = new AsyncLocal<string>();
        private const string DefaultHeaderName = "X-CorrelationId";

        /// <summary>
        /// Parameterless constructor for singleton DI registration
        /// No HTTP context dependency needed
        /// </summary>
        public CorrelationIdHelper()
        {
        }

        /// <summary>
        /// Gets the current correlation ID from AsyncLocal storage
        /// Generates a new one if none exists
        /// </summary>
        public string Get()
        {
            return _correlationId.Value ?? GenerateNewCorrelationId();
        }

        /// <summary>
        /// Sets the correlation ID in AsyncLocal storage
        /// Generates a new one if provided value is null/empty
        /// </summary>
        public void Set(string correlationId)
        {
            _correlationId.Value = string.IsNullOrEmpty(correlationId)
                ? GenerateNewCorrelationId()
                : correlationId;
        }

        /// <summary>
        /// Generates a new correlation ID and stores it in AsyncLocal
        /// </summary>
        private static string GenerateNewCorrelationId()
        {
            var newId = Guid.NewGuid().ToString();
            _correlationId.Value = newId;
            return newId;
        }

        /// <summary>
        /// Clears the current correlation ID from AsyncLocal storage
        /// Useful for background tasks or new execution contexts
        /// </summary>
        public void Clear()
        {
            _correlationId.Value = null;
        }
    }
}

