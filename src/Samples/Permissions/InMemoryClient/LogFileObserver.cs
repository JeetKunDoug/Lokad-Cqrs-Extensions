using System;
using System.Diagnostics;
using System.IO;

using Lokad.Cqrs;
using Lokad.Cqrs.Build.Engine.Events;
using Lokad.Cqrs.Feature.AtomicStorage;

using Shared;

namespace InMemoryClient
{
    public class LogFileObserver : IObserver<ISystemEvent>
    {
        private Process process;
        private readonly ConsoleReader reader;

        public LogFileObserver()
        {
            reader = new ConsoleReader();
        }

        #region Implementation of IObserver<in ISystemEvent>

        public void OnNext(ISystemEvent value)
        {
            if (value is EngineInitializationStarted)
            {
                OnEngineInitializationStarted();
            }
            if (value is EngineStopped)
            {
                OnEngineStopped();
            }
        }

        public void OnError(Exception error)
        {}

        public void OnCompleted()
        {}

        #endregion

        private void OnEngineStopped()
        {
            if (process != null && !process.HasExited)
            {
                process.Kill();
            }
            var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "storage");
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        private void OnEngineInitializationStarted()
        {
            var logFile = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs"), "log.txt");

            if (reader.Confirm("open log file?"))
            {
                process = new Process { StartInfo = new ProcessStartInfo(logFile) };
                process.Start();
            }
        }
    }
}