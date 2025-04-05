using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnedataDrive.Utils
{
    internal class LoggerFormater
    {
        private Logger logger;

        public LoggerFormater(Logger logger)
        {
            this.logger = logger;
        }

        public void Oneline(LogLevel logLevel, string operation, string status, string filePath = "UNKNOWN", string opID = "UNKNOWN")
        {
            string msg = $"OP ID: {opID} | {operation} -> {status} | path: {filePath}";
            logger.Log(logLevel, " OP ID: {opID} | {operation} -> {status} | path: {filePath}", opID, operation, status, filePath);
            //logger.Debug("abcd {operation} | path: {filePath}", operation, filePath);
        }
    }
}
