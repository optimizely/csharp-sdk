using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace OptimizelySDK.DatafileManagement
{
    public class HttpProjectConfigManager : PollingProjectConfigManager
    {
        public class Builder
        {
            private String SdkKey;
            private String Url;
            private String Format = "https://cdn.optimizely.com/datafiles/%s.json";
            private bool AutoUpdate = true;
            private TimeSpan Period = TimeSpan.FromSeconds(5);

            public Builder WithSdkKey(String sdkKey)
            {
                SdkKey = sdkKey;
                return this;
            }

            public Builder withUrl(String url)
            {
                Url = url;
                return this;
            }

            public Builder withFormat(String format)
            {
                Format = format;
                return this;
            }

            public Builder WithPollingInterval(TimeSpan period)
            {
                Period = period;

                return this;
            }
            public Builder WithAutUpdate(bool autoUpdate)
            {
                AutoUpdate = autoUpdate;

                return this;
            }

            public HttpProjectConfigManager build()
            {
                if (Url != null) {
                    return new HttpProjectConfigManager(Url, Period, AutoUpdate);
                }

                if (SdkKey == null) {
                    throw new Exception("sdkKey cannot be null", null);
                }

                Url = String.Format(Format, SdkKey);

                return new HttpProjectConfigManager(Url, Period, AutoUpdate);
            }
        }

        public HttpClient Client;
        private string Url;
        private string lastModifiedSince = String.Empty;

        public HttpProjectConfigManager(string url, TimeSpan period,  bool autoUpdate) : base(period, autoUpdate)
        {
            Client = new HttpClient();
            Url = url;
        }

        public Task OnReady {
            get {
                return _onReady;
            }
        }

        protected override ProjectConfig FetchConfig()
        {
            var request = new HttpRequestMessage {
                RequestUri = new Uri(Url),
                Method = HttpMethod.Get
            };

            // Need to check empty works or not.
            request.Headers.Add("last-modified", lastModifiedSince);

            var httpResponse = Client.SendAsync(request);
            httpResponse.Wait();

            var response = httpResponse.Result;

            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                return null;
            }
            // TODO: Initialize DataFileProjectConfig

            return null;
        }
    }
}
