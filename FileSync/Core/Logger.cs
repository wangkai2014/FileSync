using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSync.Core
{
    /// <summary>
    /// A very simple logger class.
    /// </summary>
    public class Logger
    {
        #region Fields

        private string m_logFilePath;
        private string m_exceptionLogFilePath;

        private string m_loggerName;

        #endregion

        #region Constructor

        public Logger(string loggerName)
        {
            if (!Directory.Exists("logs"))
                Directory.CreateDirectory("logs");

            m_loggerName = loggerName;
            GenerateNewLogFile();
        }

        #endregion

        #region Public methods

        public void AppendInfo(string info)
        {
            using (var sw = File.AppendText(m_logFilePath))
            {
                sw.WriteLine();
                sw.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff MM/dd/yyyy"));
                sw.WriteLine("\t" + info);
            }
        }

        public void AppendException(Exception ex)
        {
            if (ex == null)
                return;

            using (var sw = File.AppendText(m_exceptionLogFilePath))
            {
                sw.WriteLine();
                sw.WriteLine();
                sw.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff MM/dd/yyyy"));
                sw.WriteLine("---------------------------------------------------");
                sw.WriteLine(m_loggerName + " logged an exception :");
                sw.WriteLine(ex.ToString());
                sw.WriteLine("***************** INNER EXCEPTION *****************");
                sw.WriteLine(ex.InnerException?.ToString());
                sw.WriteLine("***************************************************");
                sw.WriteLine("---------------------------------------------------");
            }
        }

        #endregion

        #region Private methods

        private void GenerateNewLogFile()
        {
            m_logFilePath = Path.Combine(GlobalDefinitions.LogsDirectory, $"{m_loggerName}_info_{DateTime.Now.ToString("MM-dd-yyyy_hh-mm-ss-fff")}.log");
            m_exceptionLogFilePath = Path.Combine(GlobalDefinitions.LogsDirectory, $"{m_loggerName}_exceptions_{DateTime.Now.ToString("MM-dd-yyyy_hh-mm-ss-fff")}.log");
        }

        #endregion
    }
}
