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

        public void LogFileOP(LogLevel logLevel, string operation, string status, string filePath = "", string opID = "")
        {
            SetDefaultID(ref opID, ref filePath);
            logger.Log(logLevel, " OP ID: {opID} | {operation} -> {status} | path: {filePath}", opID, operation, status, filePath);
        }

        public void LogFileOP(LogLevel logLevel, string operation, string status, Exception e, string filePath = "", string opID = "")
        {
            SetDefaultID(ref opID, ref filePath);
            logger.Log(logLevel, " OP ID: {opID} | {operation} -> {status} | path: {filePath}\n\t{exception}", opID, operation, status, filePath, e);
        }

        public void LogFileOP(LogLevel logLevel, string operation, string status, List<string> moreInfo, string filePath = "", string opID = "")
        {
            SetDefaultID(ref opID, ref filePath);
            string moreInfoFormated = string.Join("\n\t", moreInfo);
            logger.Log(logLevel, " OP ID: {opID} | {operation} -> {status} | path: {filePath}\n\t{moreInfo:l}", opID, operation, status, filePath, moreInfoFormated);
        }

        public void LogFileOP(LogLevel logLevel, string operation, string status, Exception e, List<string> moreInfo, string filePath = "", string opID = "")
        {
            SetDefaultID(ref opID, ref filePath);
            string moreInfoFormated = string.Join("\n\t", moreInfo);
            logger.Log(logLevel, " OP ID: {opID} | {operation} -> {status} | path: {filePath}\n\t{moreInfo:l}\n\t{exception}", opID, operation, status, filePath, moreInfo, e);
        }

        private void SetDefaultID(ref string opID, ref string filePath)
        {
            if (filePath == string.Empty)
            {
                filePath = "unknown";
            }
            if (opID == string.Empty)
            {
                opID = "unknown";
            }
        }
    }
}
