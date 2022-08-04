﻿using Newtonsoft.Json;
using OptimizelySDK.AudienceConditions;
using OptimizelySDK.Logger;
using OptimizelySDK.Odp.Client;
using OptimizelySDK.Odp.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OptimizelySDK.Odp
{
    public class GraphQLManager : IGraphQLManager
    {
        private readonly ILogger Logger;
        private readonly IOdpClient OdpClient;

        public GraphQLManager(ILogger logger = null, IOdpClient odpClient = null)
        {
            Logger = logger ?? new DefaultLogger();
            OdpClient = odpClient ?? new OdpClient(Logger);
        }

        public string[] FetchSegments(string apiKey, string apiHost, string userKey,
            string userValue, List<string> segmentToCheck
        )
        {
            var parameters = new QuerySegmentsParameters
            {
                ApiKey = apiKey,
                ApiHost = apiHost,
                UserKey = userKey,
                UserValue = userValue,
                SegmentToCheck = segmentToCheck
            };

            string segmentsResponseJson;
            try
            {
                segmentsResponseJson = OdpClient.QuerySegments(parameters);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.WARN, ex.Message);

                return new string[0];
            }

            var response = ParseSegmentsResponseJson(segmentsResponseJson);

            if (response is null)
            {
                Logger.Log(LogLevel.WARN, "Error while parsing response.");

                return new string[0];
            }

            if (response.HasErrors)
            {
                var message = string.Join(";", response.Errors.Select(e => e.ToString()));
                Logger.Log(LogLevel.WARN, message);

                return new string[0];
            }

            return response.Data?.Customer?.Audiences?.Edges?.
                Where(e => e.Node.State == BaseCondition.QUALIFIED).
                Select(e => e.Node.Name).ToArray();
        }

        public Response ParseSegmentsResponseJson(string jsonResponse)
        {
            var json = Regex.Replace(jsonResponse, @"\s+", string.Empty);

            return JsonConvert.DeserializeObject<Response>(json);
        }
    }
}