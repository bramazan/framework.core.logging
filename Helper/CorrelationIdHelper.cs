using System;
using Microsoft.AspNetCore.Http;

namespace Framework.Core.Logging.Helper
{
    public class CorrelationIdHelper : ICorrelationIdHelper
    {
        private string? RefId;
        private const string RefIdKey = "X-CorrelationId";

        private readonly IHttpContextAccessor _httpContextAcccessor;

        public CorrelationIdHelper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAcccessor = httpContextAccessor;
        }

        public string Get()
        {
            if (Contains())
            {
                return _httpContextAcccessor?.HttpContext?.Request.Headers[RefIdKey].ToString() ?? Guid.NewGuid().ToString();
            }
            else
            {
                RefId = Guid.NewGuid().ToString();
                SetKeyHeader();
                return RefId;
            }
        }

        public void Set(string correlationId)
        {
            if (string.IsNullOrEmpty(correlationId))
            {
                RefId = Guid.NewGuid().ToString();
            }
            else
            {
                RefId = correlationId;
            }

            SetKeyHeader();
        }

        private bool Contains()
        {
            return _httpContextAcccessor?.HttpContext?.Request?.Headers?.ContainsKey(RefIdKey) ?? false;
        }

        private void SetKeyHeader()
        {
            if (_httpContextAcccessor?.HttpContext != null)
            {
                if (Contains())
                {
                    _httpContextAcccessor.HttpContext.Request.Headers.Remove(RefIdKey);
                }

                if (!string.IsNullOrEmpty(RefId))
                {
                    _httpContextAcccessor.HttpContext.Request.Headers.Add(RefIdKey, RefId);
                }
            }
        }
    }
}

