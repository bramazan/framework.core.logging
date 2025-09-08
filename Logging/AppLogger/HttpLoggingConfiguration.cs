using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Framework.Core.Logging.Logging.AppLogger
{
    public class HttpLoggingConfiguration
    {
        public bool LogRequests { get; set; } = true;
        public bool LogResponses { get; set; } = true;
        public bool LogHttpClient { get; set; } = true;
        public bool LogMethodEntryExit { get; set; } = false;
        public int MaxBodySize { get; set; } = 180 * 1024; // 180KB
        public int MaxHeaderSize { get; set; } = 20 * 1024; // 20KB
        public List<string> SensitiveHeaders { get; set; } = new() { "Authorization", "Cookie", "Set-Cookie" };
        public List<string> SensitiveFields { get; set; } = new() { "password", "cardNumber", "pin", "cvv", "clientSecret" };
        public List<string> IgnoredPaths { get; set; } = new() { "/health", "/swagger", "/favicon.ico" };

        public static HttpLoggingConfiguration Create(IConfiguration configuration)
        {
            var config = new HttpLoggingConfiguration();
            var section = configuration.GetSection("Logging:HttpLogging");
            
            if (section.Exists())
            {
                config.LogRequests = section.GetValue("LogRequests", true);
                config.LogResponses = section.GetValue("LogResponses", true);
                config.LogHttpClient = section.GetValue("LogHttpClient", true);
                config.LogMethodEntryExit = section.GetValue("LogMethodEntryExit", false);
                config.MaxBodySize = section.GetValue("MaxBodySize", 180 * 1024);
                config.MaxHeaderSize = section.GetValue("MaxHeaderSize", 20 * 1024);
                
                var sensitiveHeaders = section.GetSection("SensitiveHeaders").Get<List<string>>();
                if (sensitiveHeaders != null && sensitiveHeaders.Count > 0)
                {
                    config.SensitiveHeaders = sensitiveHeaders;
                }
                
                var sensitiveFields = section.GetSection("SensitiveFields").Get<List<string>>();
                if (sensitiveFields != null && sensitiveFields.Count > 0)
                {
                    config.SensitiveFields = sensitiveFields;
                }
                
                var ignoredPaths = section.GetSection("IgnoredPaths").Get<List<string>>();
                if (ignoredPaths != null && ignoredPaths.Count > 0)
                {
                    config.IgnoredPaths = ignoredPaths;
                }
            }
            
            return config;
        }
    }
}