using NLog;
using OnedataDrive.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Vanara.PInvoke.CldApi;

namespace OnedataDrive
{
    public class PlaceholderFetcher
    {
        private Logger logger;
        private LoggerFormater loggerFormater;
        public PlaceholderFetcher(Logger logger)
        {
            this.logger = logger;
            this.loggerFormater = new(logger);
        }

        public void FetchPlaceholders(Callback callback)
        {

        }
    }
}
