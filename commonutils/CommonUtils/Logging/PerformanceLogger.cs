using log4net;
using System;
using System.Diagnostics;
using System.Net.Http;
using static CommonUtils.Properties.Resources;

namespace CommonUtils.Logging
{
    public sealed class PerformanceLogger : IDisposable
    {
        private const string peformanceLoggerName = "Performance";
        private ILog logger = LogManager.GetLogger(peformanceLoggerName);

        private readonly Stopwatch stopwatch;
        private readonly string methodName;

        private PerformanceLogger()
        {
            this.stopwatch = Stopwatch.StartNew();
        }

        public PerformanceLogger(HttpMethod httpMethod, Uri uri)
            : this()
        {
            if (httpMethod == null)
                throw new ArgumentNullException(nameof(httpMethod));

            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            this.methodName = httpMethod.Method + " " + uri.AbsoluteUri;
        }

        public PerformanceLogger(string methodName)
            : this()
        {
            this.methodName = methodName;
        }

        public void Dispose()
        {
            stopwatch.Stop();
            logger.InfoFormat(PerformanceLogMessage, this.methodName, this.stopwatch.ElapsedMilliseconds);
        }
    }
}