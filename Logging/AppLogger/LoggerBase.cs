using System;
namespace Framework.Core.Logging.Logging.AppLogger
{
    public abstract class LoggerBase : IDisposable
    {

        private readonly string _name;

        private bool _debugMode;
        private bool _consoleEnabled;
        private bool _disposing;

        private ulong queued = 0;
        private ulong success = 0;
        private ulong fail = 0;

        private readonly DateTime start = DateTime.Now;

        protected LoggerBase(string name, bool? ConsoleEnabled, bool? DebugMode)
        {
            _name = name;
            UpdateConsoleEnabled(ConsoleEnabled);
            UpdateDebugMode(DebugMode);

            if (_debugMode)
            {
                Task.Factory.StartNew(() => WriteToCsv());
            }

            PushToQueue = (string s) =>
            {
                Interlocked.Increment(ref queued);
                if (_consoleEnabled) Console.Out.WriteLine(s);
            };
        }

        public Action<string> PushToQueue { get; set; }

        protected void UpdateDebugMode(bool? DebugMode)
        {
            _debugMode = DebugMode ?? false;
        }

        protected void UpdateConsoleEnabled(bool? ConsoleEnabled)
        {
            _consoleEnabled = ConsoleEnabled ?? false;
        }

        private async Task WriteToCsv()
        {
            while (true)
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        Console.Out.WriteLine($"++ {_name}: total queued/success/failure:{queued}/{success}/{fail}");
                        System.IO.File.AppendAllLines($"points_{_name}.csv", new string[] { $"{Math.Round((DateTime.Now - start).TotalSeconds, 0)};{queued};{success};{fail}" });
                    }
                    catch (Exception ex)
                    {
                        Console.Out.WriteLine($"{_name}: Error while writing to csv: {ex}");
                    }
                });
                if (_disposing) { break; }
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        private readonly object syncLock = new();


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            //kalan son loglarin gonderimi icin
            _disposing = true;
        }
    }
}

