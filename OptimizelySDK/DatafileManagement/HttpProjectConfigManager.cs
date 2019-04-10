using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace OptimizelySDK.DatafileManagement
{
    public class HttpProjectConfigManager : PollingProjectConfigManager
    {
        public class Builder
        {
            private string SdkKey;
            private string Url;
            private string Format = "https://cdn.optimizely.com/json/{0}.json";
            private bool AutoUpdate = true;
            private TimeSpan Period = TimeSpan.FromSeconds(5);
            private ILogger Logger;
            private IErrorHandler ErrorHandler;

            public Builder WithSdkKey(string sdkKey)
            {
                SdkKey = sdkKey;
                return this;
            }

            public Builder WithUrl(string url)
            {
                Url = url;
                return this;
            }

            public Builder WithFormat(string format)
            {
                Format = format;
                return this;
            }

            public Builder WithPollingInterval(TimeSpan period)
            {
                Period = period;
                return this;
            }

            public Builder WithAutoUpdate(bool autoUpdate)
            {
                AutoUpdate = autoUpdate;
                return this;
            }

            public Builder WithLogger(ILogger logger)
            {
                Logger = logger;
                return this;
            }

            public Builder WithErrorHandler(IErrorHandler errorHandler)
            {
                ErrorHandler = errorHandler;
                return this;
            }

            public HttpProjectConfigManager Build()
            {
                //if (Url != null)
                //    return new HttpProjectConfigManager(Url, Period, AutoUpdate);

                if (SdkKey == null)
                    throw new Exception("sdkKey cannot be null");

                Url = string.Format(Format, SdkKey);
                return new HttpProjectConfigManager(Url, Period, AutoUpdate, Logger, ErrorHandler);
            }
        }

        public HttpClient Client;
        private string Url;
        private string lastModifiedSince = string.Empty;
        
        public HttpProjectConfigManager(string url, TimeSpan period,  bool autoUpdate, ILogger logger, IErrorHandler errorHandler)
            : base(period, autoUpdate, logger, errorHandler)
        {
            Client = new HttpClient();
            Url = url;
            Logger = logger;
            ErrorHandler = errorHandler;
        }

        public Task OnReady
        {
            get
            {
                return _onReady;
            }
        }

        protected override ProjectConfig FetchConfig()
        {
            //Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(Url),
                Method = HttpMethod.Get,
            };

            //request.Content = new StringContent("", Encoding.UTF8, "application/json");

            //request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            //request.Headers.Add("Content-Type", "application/json");

            // Send If-Modified-Since header if Last-Modified-Since header contains any value.
            if (!string.IsNullOrEmpty(lastModifiedSince))
                request.Headers.Add("If-Modified-Since", lastModifiedSince);

            var httpResponse = Client.SendAsync(request);
            httpResponse.Wait();

            var response = httpResponse.Result;
            var responseHeaders = response.Headers;
            if (responseHeaders.TryGetValues("Last-Modified", out IEnumerable<string> values))
                lastModifiedSince = values.First();
            
            // Return from here if datafile is not modified.
            if (response.StatusCode == HttpStatusCode.NotModified)
                return null;

            var content = response.Content.ReadAsStringAsync();
            content.Wait();

            // TODO: Initialize DataFileProjectConfig
            string datafile = content.Result.ToString();
            var projectConfig = DatafileProjectConfig.Create(datafile, null, null);

            return projectConfig;
        }
    }
}
