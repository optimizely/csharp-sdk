using OptimizelySDK.Logger;
using System;
using System.Collections.Generic;

namespace OptimizelySDK.Odp
{
    public class OdpSegmentManager
    {
        private readonly ILogger _logger;
        private readonly IOdpSegmentApiManager _apiManager;
        private readonly IOdpConfig _odpConfig;

        private readonly Cache<List<string>> _segmentsCache;

        public OdpSegmentManager(IOdpConfig odpConfig, IOdpSegmentApiManager apiManager,
            int cacheSize = Constants.DEFAULT_MAX_CACHE_SIZE, TimeSpan? itemTimeout = default,
            ILogger logger = null
        )
        {
            _apiManager = apiManager;
            _odpConfig = odpConfig;
            _logger = logger ?? new DefaultLogger();

            var timeout = itemTimeout ?? TimeSpan.FromMinutes(Constants.DEFAULT_CACHE_MINUTES);
            if (timeout < TimeSpan.Zero)
            {
                _logger.Log(LogLevel.WARN,
                    "Negative item timeout provided. Items will not expire in cache.");
                timeout = TimeSpan.Zero;
            }

            _segmentsCache = new LruCache<List<string>>(cacheSize, timeout, logger);
        }

        public List<string> GetQualifiedSegments(string fsUserId, List<OdpSegmentOption> options = null)
        {
            if (!_odpConfig.IsReady())
            {
                _logger.Log(LogLevel.ERROR, "Audience segments fetch failed (ODP is not enabled)");
                return new List<string>();
            }

            if (!_odpConfig.HasSegments())
            {
                _logger.Log(LogLevel.DEBUG,
                    "No Segments are used in the project, Not Fetching segments. Returning empty list");
                return new List<string>();
            }
            
            List<string> qualifiedSegments;
            var cacheKey = GetCacheKey(OdpUserKeyType.FS_USER_ID.ToString().ToLower(), fsUserId);

            if (options?.Contains(OdpSegmentOption.ResetCache))
            {
                _segmentsCache.reset();
            }
            else if (!options.Contains(OdpSegmentOption.IgnoreCache))
            {
                qualifiedSegments = _segmentsCache.lookup(cacheKey);
                if (qualifiedSegments != null)
                {
                    _logger.Log(LogLevel.DEBUG,"ODP Cache Hit. Returning segments from Cache.");
                    return qualifiedSegments;
                }
            }

            _logger.Log(LogLevel.DEBUG, "ODP Cache Miss. Making a call to ODP Server.");

            var parser = ResponseJsonParserFactory.getParser();
            var qualifiedSegmentsResponse = _apiManager.FetchSegments(
                _odpConfig.ApiKey,
                _odpConfig.ApiHost + Constants.ODP_GRAPHQL_API_ENDPOINT_PATH,
                OdpUserKeyType.FS_USER_ID, fsUserId, _odpConfig.SegmentsToCheck);
            qualifiedSegments = parser.parseQualifiedSegments(qualifiedSegmentsResponse);

            if (qualifiedSegments != null && !options.Contains(OdpSegmentOption.IgnoreCache))
            {
                _segmentsCache.save(cacheKey, qualifiedSegments);
            }

            return qualifiedSegments;
        }

        private static string GetCacheKey(string userKey, string userValue)
        {
            return userKey + "-$-" + userValue;
        }
    }
}
