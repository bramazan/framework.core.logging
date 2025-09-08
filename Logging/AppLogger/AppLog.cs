using System;
namespace Framework.Core.Logging.Logging.AppLogger
{
	public class AppLog
    {
        public DateTimeOffset Timestamp;
        public string Level;
        public string MessageTemplate;
        public Dictionary<string, object> Properties;
    }
}