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
            logger.Log(logLevel, " OP ID: {opID} | {operation} -> {status} | path: {filePath}", opID, operation, status, filePath);
        }

        public void Oneline(LogLevel logLevel, string operation, string status, Exception e, string filePath = "UNKNOWN", string opID = "UNKNOWN")
        {
            logger.Log(logLevel, " OP ID: {opID} | {operation} -> {status} | path: {filePath}\n\t{exception}", opID, operation, status, filePath, e);
        }
    }
}
