using NLog;

namespace OnedataDrive.Utils
{
    internal class LoggerFormater
    {
        private Logger logger;

        public LoggerFormater(Logger logger)
        {
            this.logger = logger;
        }

        public void Oneline(LogLevel logLevel, string operation, string status, string filePath = "", string opID = "")
        {
            if (filePath == string.Empty)
            {
                filePath = "unknown";
            }
            if (opID == string.Empty)
            {
                opID = "unknown";
            }
            logger.Log(logLevel, " OP ID: {opID} | {operation} -> {status} | path: {filePath}", opID, operation, status, filePath);
        }

        public void Oneline(LogLevel logLevel, string operation, string status, Exception e, string filePath = "", string opID = "")
        {
            if (filePath == string.Empty)
            {
                filePath = "unknown";
            }
            if (opID == string.Empty)
            {
                opID = "unknown";
            }
            logger.Log(logLevel, " OP ID: {opID} | {operation} -> {status} | path: {filePath}\n\t{exception}", opID, operation, status, filePath, e);
        }
    }
}
