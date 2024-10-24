using System;
using System.Collections.Generic;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.OptimizelyDecisions;

namespace OptimizelySDK.Bucketing
{
    public class UserProfileTracker
    {
        public UserProfile UserProfile { get; private set; }
        public bool ProfileUpdated { get; private set; }

        private readonly UserProfileService _userProfileService;
        private readonly string _userId;
        private readonly ILogger _logger;
        private readonly IErrorHandler _errorHandler;

        public UserProfileTracker(UserProfileService userProfileService, string userId, ILogger logger, IErrorHandler errorHandler)
        {
            _userProfileService = userProfileService;
            _userId = userId;
            _logger = logger;
            _errorHandler = errorHandler;
            ProfileUpdated = false;
            UserProfile = null;
        }

        public void LoadUserProfile(DecisionReasons reasons)
        {
            try
            {
                var userProfileMap = _userProfileService.Lookup(_userId);
                if (userProfileMap == null)
                {
                    _logger.Log(LogLevel.INFO,
                        reasons.AddInfo(
                            "We were unable to get a user profile map from the UserProfileService."));
                }
                else if (UserProfileUtil.IsValidUserProfileMap(userProfileMap))
                {
                    UserProfile = UserProfileUtil.ConvertMapToUserProfile(userProfileMap);
                }
                else
                {
                    _logger.Log(LogLevel.WARN,
                        reasons.AddInfo("The UserProfileService returned an invalid map."));
                }
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.ERROR, reasons.AddInfo(exception.Message));
                _errorHandler.HandleError(
                    new Exceptions.OptimizelyRuntimeException(exception.Message));
            }

            if (UserProfile == null)
            {
                UserProfile = new UserProfile(_userId, new Dictionary<string, Decision>());
            }
        }

        public void UpdateUserProfile(Experiment experiment, Variation variation)
        {
            var experimentId = experiment.Id;
            var variationId = variation.Id;
            Decision decision;
            if (UserProfile.ExperimentBucketMap.ContainsKey(experimentId))
            {
                decision = UserProfile.ExperimentBucketMap[experimentId];
                decision.VariationId = variationId;
            }
            else
            {
                decision = new Decision(variationId);
            }

            UserProfile.ExperimentBucketMap[experimentId] = decision;
            ProfileUpdated = true;

            _logger.Log(LogLevel.INFO,
                $"Saved variation \"{variationId}\" of experiment \"{experimentId}\" for user \"{UserProfile.UserId}\".");
        }

        public void SaveUserProfile()
        {
            if (!ProfileUpdated)
            {
                return;
            }

            try
            {
                _userProfileService.Save(UserProfile.ToMap());
                _logger.Log(LogLevel.INFO,
                    $"Saved user profile of user \"{UserProfile.UserId}\".");
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.WARN,
                    $"Failed to save user profile of user \"{UserProfile.UserId}\".");
                _errorHandler.HandleError(
                    new Exceptions.OptimizelyRuntimeException(exception.Message));
            }
        }
    }
}
