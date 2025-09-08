using System;
using Microsoft.Extensions.Configuration;

namespace Framework.Core.Logging.Logging.AppLogger
{
    public class PlatformAppLoggerConfiguration
    {
        public PlatformAppLoggerConfiguration()
        {

        }

        public static PlatformAppLoggerConfiguration Create(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var conf = new PlatformAppLoggerConfiguration
            {
                LoggerApplicationName = configuration["Logging.ApplicationName"] ?? $"{MachineName}-{EnvironmentName}",
                ConsoleEnabled = GetConsoleLoggingState(configuration["Logging.ConsoleEnabled"] ?? string.Empty, EnvironmentName),
                DebugMode = GetDebugMode(configuration["Logging.DebugMode"] ?? string.Empty),
                HttpLogging = HttpLoggingConfiguration.Create(configuration)
            };

            conf.Enabled = conf.ConsoleEnabled;
            return conf;
        }

        public static string MachineName => Environment.MachineName ?? "Unknown";

        public static string EnvironmentName
        {
            get
            {
                var _environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
                if (_environmentName.ToLower() == "test") _environmentName = "Staging";
                return _environmentName;
            }
        }


        public bool ConsoleEnabled { get; set; }

        public bool DebugMode { get; set; }

        public bool Enabled { get; set; }

        public string LoggerApplicationName { get; set; } = string.Empty;

        public HttpLoggingConfiguration HttpLogging { get; set; } = new();

        private static bool GetConsoleLoggingState(string consoleEnabledConfig, string EnvironmentName)
        {
            if (bool.TryParse(consoleEnabledConfig, out bool result))
            {
                return result;
            }
            else
            {
                return EnvironmentName.ToLower() switch
                {
                    "staging" => false,
                    "production" => false,
                    _ => true,
                };
            }
        }

        private static bool GetDebugMode(string debugModeConfig)
        {
            if (bool.TryParse(debugModeConfig, out bool result))
            {
                return result;
            }
            else
            {
                return false;
            }
        }
    }
}

