/* 
 * Copyright 2017, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using OptimizelySDK.Bucketing;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event.Builder;
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.Logger;
using OptimizelySDK.Utils;
using System;
using System.Collections.Generic;

namespace OptimizelySDK
{
    public class Optimizely
    {

        private Bucketer Bucketer;

        private EventBuilder EventBuilder;

        private IEventDispatcher EventDispatcher;
        
        private ProjectConfig Config;

        private ILogger Logger;

        private IErrorHandler ErrorHandler;

        private UserProfileService UserProfileService;

        private DecisionService DecisionService;

        public bool IsValid { get; private set; }


        /// <summary>
        /// Optimizely constructor for managing Full Stack .NET projects.
        /// </summary>
        /// <param name="datafile">string JSON string representing the project</param>
        /// <param name="eventDispatcher">EventDispatcherInterface</param>
        /// <param name="logger">LoggerInterface</param>
        /// <param name="errorHandler">ErrorHandlerInterface</param>
        /// <param name="skipJsonValidation">boolean representing whether JSON schema validation needs to be performed</param>
        public Optimizely(string datafile,
                          IEventDispatcher eventDispatcher = null,
                          ILogger logger = null,
                          IErrorHandler errorHandler = null,
                          UserProfileService userProfileService = null,
                          bool skipJsonValidation = false)
        {
            IsValid = false; // invalid until proven valid
            Logger = logger ?? new NoOpLogger();
            EventDispatcher = eventDispatcher ?? new DefaultEventDispatcher(Logger);
            ErrorHandler = errorHandler ?? new NoOpErrorHandler();
            Bucketer = new Bucketer(Logger);
            EventBuilder = new EventBuilder(Bucketer);
            UserProfileService = userProfileService;

            try
            {
                if (!ValidateInputs(datafile, skipJsonValidation))
                {
                    Logger.Log(LogLevel.ERROR, "Provided 'datafile' has invalid schema.");
                    return;
                }

                Config = ProjectConfig.Create(datafile, Logger, ErrorHandler);
                IsValid = true;
                DecisionService = new DecisionService(Bucketer, ErrorHandler, Config, userProfileService, Logger);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.ERROR, "Provided 'datafile' is in an invalid format. " + ex.Message);
            }
        }


        /// <summary>
        /// Helper function to validate all required conditions before performing activate or track.
        /// </summary>
        /// <param name="experiment">Experiment Object representing experiment</param>
        /// <param name="userId">string ID for user</param>
        /// <param name="userAttributes">associative array of Attributes for the user</param>
        private bool ValidatePreconditions(Experiment experiment, string userId, UserAttributes userAttributes = null)
        {
            if (!experiment.IsExperimentRunning)
            {
                Logger.Log(LogLevel.INFO, string.Format("Experiment {0} is not running.", experiment.Key));
                return false;
            }

            if (experiment.IsUserInForcedVariation(userId))
            {
                return true;
            }

            if (!ExperimentUtils.IsUserInExperiment(Config, experiment, userAttributes))
            {
                Logger.Log(LogLevel.INFO, string.Format("User \"{0}\" does not meet conditions to be in experiment \"{1}\".", userId, experiment.Key));
                return false;
            }

            return true;
        }


        /// <summary>
        /// Buckets visitor and sends impression event to Optimizely.
        /// </summary>
        /// <param name="experimentKey">experimentKey string Key identifying the experiment</param>
        /// <param name="userId">string ID for user</param>
        /// <param name="attributes">associative array of Attributes for the user</param>
        /// <returns>null|string Representing variation</returns>
        public string Activate(string experimentKey, string userId, UserAttributes userAttributes = null)
        {
            if (!IsValid)
            {
                Logger.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'activate'.");
                return null;
            }

            var experiment = Config.GetExperimentFromKey(experimentKey);

            if (experiment.Key == null)
            {
                Logger.Log(LogLevel.INFO, string.Format("Not activating user {0}.", userId));
                return null;
            }

            var variation = DecisionService.GetVariation(experiment, userId, userAttributes);

            if (variation == null || variation.Key == null)
            {
                Logger.Log(LogLevel.INFO, string.Format("Not activating user {0}.", userId));
                return null;
            }

            if (userAttributes != null) {
                userAttributes = userAttributes.FilterNullValues(Logger);
            }

            var impressionEvent = EventBuilder.CreateImpressionEvent(Config, experiment, variation.Id, userId, userAttributes);
            Logger.Log(LogLevel.INFO, string.Format("Activating user {0} in experiment {1}.", userId, experimentKey));
            Logger.Log(LogLevel.DEBUG, string.Format("Dispatching impression event to URL {0} with params {1}.", 
                impressionEvent, impressionEvent.GetParamsAsJson()));

            try
            {
                EventDispatcher.DispatchEvent(impressionEvent);
            }
            catch (Exception exception)
            {
                Logger.Log(LogLevel.ERROR, string.Format("Unable to dispatch impression event. Error {0}", exception.Message));
            }

            return variation.Key;
        }

        /// <summary>
        /// Validate datafile
        /// </summary>
        /// <param name="datafile">string JSON string representing the project.</param>
        /// <param name="skipJsonValidation">whether JSON schema validation needs to be performed</param>
        /// <returns>true iff all provided inputs are valid</returns>
        private bool ValidateInputs(string datafile, bool skipJsonValidation)
        {
            return skipJsonValidation || Validator.ValidateJSONSchema(datafile);
        }


        /// <summary>
        /// Sends conversion event to Optimizely.
        /// </summary>
        /// <param name="eventKey">Event key representing the event which needs to be recorded</param>
        /// <param name="userId">ID for user</param>
        /// <param name="userAttributes">Attributes of the user</param>
        /// <param name="eventTags">eventTags array Hash representing metadata associated with the event.</param>
        public void Track(string eventKey, string userId, UserAttributes userAttributes = null, EventTags eventTags = null)
        {
            if (!IsValid)
            {
                Logger.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'track'.");
                return;
            }

            var eevent = Config.GetEvent(eventKey);

            if (eevent.Key == null)
            {
                Logger.Log(LogLevel.ERROR, string.Format("Not tracking user {0} for event {1}.", userId, eventKey));
                return;
            }

            // Filter out experiments that are not running or when user(s) do not meet conditions.
            var validExperiments = new List<Experiment>();
            var experimentIds = eevent.ExperimentIds;
            foreach (string id in eevent.ExperimentIds)
            {
                var experiment = Config.GetExperimentFromId(id);
                //Validate experiment
                var variation = DecisionService.GetVariation(experiment, userId, userAttributes);
                if (variation != null)
                {
                    validExperiments.Add(experiment);
                }
                else
                {
                    Logger.Log(LogLevel.INFO, string.Format("Not tracking user \"{0}\" for experiment \"{1}\"", userId, experiment.Key));
                }
            }

            if (validExperiments.Count > 0)
            {

                if (userAttributes != null)
                {
                    userAttributes = userAttributes.FilterNullValues(Logger);
                }

                if (eventTags != null)
                {
                    eventTags = eventTags.FilterNullValues(Logger);
                }

                var conversionEvent = EventBuilder.CreateConversionEvent(Config, eventKey, validExperiments,
                    userId, userAttributes, eventTags);
                Logger.Log(LogLevel.INFO, string.Format("Tracking event {0} for user {1}.", eventKey, userId));
                Logger.Log(LogLevel.DEBUG, string.Format("Dispatching conversion event to URL {0} with params {1}.", 
                    conversionEvent.Url, conversionEvent.GetParamsAsJson()));

                try
                {
                    EventDispatcher.DispatchEvent(conversionEvent);
                }
                catch (Exception exception)
                {
                    Logger.Log(LogLevel.ERROR, string.Format("Unable to dispatch conversion event. Error {0}", exception.Message));
                }

            }
            else
            {
                Logger.Log(LogLevel.INFO, string.Format("There are no valid experiments for event {0} to track.", eventKey));
            }
        }


        /// <summary>
        /// Get variation where user will be bucketed
        /// </summary>
        /// <param name="experimentKey">experimentKey string Key identifying the experiment</param>
        /// <param name="userId">ID for the user</param>
        /// <param name="userAttributes">Attributes for the users</param>
        /// <returns>null|string Representing variation</returns>
        public string GetVariation(string experimentKey, string userId, UserAttributes userAttributes = null)
        {
            if (!IsValid)
            {
                Logger.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'GetVariation'.");
                return null;
            }

            Experiment experiment = Config.GetExperimentFromKey(experimentKey);
            if (experiment.Key == null)
                return null;

            Variation variation = DecisionService.GetVariation(experiment, userId, userAttributes);
            return variation == null ? null : variation.Key;
        }

        /// <summary>
        /// Force a user into a variation for a given experiment.
        /// </summary>
        /// <param name="experimentKey">The experiment key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="variationKey">The variation key specifies the variation which the user will be forced into.
        /// If null, then clear the existing experiment-to-variation mapping.</param>
        /// <returns>A boolean value that indicates if the set completed successfully.</returns>
        public bool SetForcedVariation(string experimentKey, string userId, string variationKey)
        {
            return Config.SetForcedVariation(experimentKey, userId, variationKey);
        }

        /// <summary>
        /// Gets the forced variation key for the given user and experiment.  
        /// </summary>
        /// <param name="experimentKey">The experiment key</param>
        /// <param name="userId">The user ID</param>
        /// <returns>null|string The variation key.</returns>
        public string GetForcedVariation(string experimentKey, string userId)
        {
            var forcedVariation = Config.GetForcedVariation(experimentKey, userId);

            return forcedVariation != null ? forcedVariation.Key : null;
        }
    }
}
