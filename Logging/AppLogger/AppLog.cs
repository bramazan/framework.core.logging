using System;
using System.Collections.Generic;

namespace Framework.Core.Logging.Logging.AppLogger
{
	public class AppLog
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string MessageTemplate { get; set; } = string.Empty;
        public Dictionary<string, object?> Properties { get; set; } = new();
    }
}