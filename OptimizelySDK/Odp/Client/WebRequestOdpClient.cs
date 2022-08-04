using OptimizelySDK.Logger;
using OptimizelySDK.Odp.Entity;
using System.Net;
using System;
using System.IO;

namespace OptimizelySDK.Odp.Client
{
    public class WebRequestOdpClient : IOdpClient
    {
        public ILogger Logger { get; set; } = new DefaultLogger();
        
        private HttpWebRequest Request = null;
        
        public string QuerySegments(QuerySegmentsParameters parameters)
        {
            Request = (HttpWebRequest)WebRequest.Create(parameters.ApiHost);
            Request.Method = WebRequestMethods.Http.Post;
            Request.Headers.Add("x-api-key", parameters.ApiKey);
            Request.ContentType = "application/json";

            using (var streamWriter = new StreamWriter(Request.GetRequestStream()))
            {
                streamWriter.Write(parameters.ToJson());
                streamWriter.Flush();
                streamWriter.Close();
            }

            var result = Request.BeginGetResponse(FinalizeHttpAsyncRequest, this);

            return result.ToString();
        }

        private static void FinalizeHttpAsyncRequest(IAsyncResult result)
        {
            ((WebRequestOdpClient)result.AsyncState).FinalizeRequest(result);
        }

        private void FinalizeRequest(IAsyncResult result)
        {
            var response = (HttpWebResponse)Request.EndGetResponse(result);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseStream = response.GetResponseStream();
                var streamEncoder = System.Text.Encoding.UTF8;
                
                if (responseStream == null)
                {
                    return;
                }

                var responseReader = new StreamReader(responseStream, streamEncoder);
                responseReader.ReadToEnd();
            }
            else
            {
                Logger.Log(LogLevel.WARN, "Received HTTP status code other than 200. Unable get ODP data.");
            }
        }
    }
}