using System;
using System.IO;
using System.Text;

namespace DrvFR_Daemon
{
    public class Logger
    {
        private readonly object _lock = new object();
        private readonly string _logFile;

        public Logger(string logFile)
        {
            if (string.IsNullOrWhiteSpace(logFile))
                throw new ArgumentException("Log file path cannot be empty");

            _logFile = logFile;

            // Создаём каталог, если его нет
            var dir = Path.GetDirectoryName(_logFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public void Write(string text)
        {
            lock (_lock)
            {
                text = string.Format("{0:HH:mm:ss.fff}\t{1}", DateTime.Now, text);
                File.AppendAllText(_logFile, text + Environment.NewLine, Encoding.UTF8);
            }
        }

        public void Separator()
        {
            Write(new string('-', 60));
        }

        public void OpenFile()
        {
            if (!File.Exists(_logFile))
                File.WriteAllText(_logFile, "");
            System.Diagnostics.Process.Start(_logFile);
        }
    }
}
