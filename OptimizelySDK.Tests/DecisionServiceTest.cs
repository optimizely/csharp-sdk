/**
 *
 *    Copyright 2017-2019, Optimizely and contributors
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 */
using System;
using System.Collections.Generic;
using Moq;
using OptimizelySDK.Logger;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Entity;
using NUnit.Framework;
using OptimizelySDK.Bucketing;
using OptimizelySDK.Utils;
using OptimizelySDK.Config;

namespace OptimizelySDK.Tests
{
    public class DecisionServiceTest
    {
        private string GenericUserId = "genericUserId";
        private string WhitelistedUserId = "testUser1";
        private string UserProfileId = "userProfileId";
        private Mock<ILogger> LoggerMock;
        private Mock<IErrorHandler> ErrorHandlerMock;
        private Mock<UserProfileService> UserProfileServiceMock;
        private Mock<Bucketer> BucketerMock;
        private Mock<DecisionService> DecisionServiceMock;
        
        private ProjectConfig ProjectConfig;
        private Experiment WhitelistedExperiment;
        private Variation WhitelistedVariation;
        private DecisionService DecisionService;

        private Variation VariationWithKeyControl;
        private Variation VariationWithKeyVariation;

        [SetUp]
        public void SetUp()
        {
            LoggerMock              = new Mock<ILogger>();
            ErrorHandlerMock        = new Mock<IErrorHandler>();
            UserProfileServiceMock  = new Mock<UserProfileService>();
            BucketerMock            = new Mock<Bucketer>(LoggerMock.Object);
            
            ProjectConfig           = DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object, ErrorHandlerMock.Object);
            WhitelistedExperiment   = ProjectConfig.ExperimentIdMap["224"];
            WhitelistedVariation    = WhitelistedExperiment.VariationKeyToVariationMap["vtag5"];
            
            DecisionService         = new DecisionService(new Bucketer(LoggerMock.Object), ErrorHandlerMock.Object, ProjectConfig, null, LoggerMock.Object);
            DecisionServiceMock     = new Mock<DecisionService>(BucketerMock.Object, ErrorHandlerMock.Object, ProjectConfig, null, LoggerMock.Object) { CallBase = true };

            VariationWithKeyControl     = ProjectConfig.GetVariationFromKey("test_experiment", "control");
            VariationWithKeyVariation   = ProjectConfig.GetVariationFromKey("test_experiment", "variation");
        }

        [Test]
        public void TestGetVariationForcedVariationPrecedesAudienceEval()
        {
            DecisionService decisionService = new DecisionService(BucketerMock.Object, ErrorHandlerMock.Object, ProjectConfig, null, LoggerMock.Object);
            Experiment experiment = ProjectConfig.Experiments[8];
            Variation expectedVariation = experiment.Variations[0];

            // user excluded without audiences and whitelisting
            Assert.IsNull(decisionService.GetVariation(experiment, GenericUserId, new UserAttributes()));

            var actualVariation = decisionService.GetVariation(experiment, WhitelistedUserId, new UserAttributes());

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, string.Format("User \"{0}\" is forced in variation \"vtag5\".", WhitelistedUserId)), Times.Once);
            // no attributes provided for a experiment that has an audience
            Assert.IsTrue(TestData.CompareObjects(actualVariation, expectedVariation));
            BucketerMock.Verify(_ => _.Bucket(It.IsAny<ProjectConfig>(), It.IsAny<Experiment>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void TestGetVariationEvaluatesUserProfileBeforeAudienceTargeting()
        {
            Experiment experiment = ProjectConfig.Experiments[8];
            Variation variation = experiment.Variations[0];

            Decision decision = new Decision(variation.Id);
            UserProfile userProfile = new UserProfile(UserProfileId, new Dictionary<string, Decision>
            {
                { experiment.Id, decision }
            });

            UserProfileServiceMock.Setup(up => up.Lookup(WhitelistedUserId)).Returns(userProfile.ToMap());

            DecisionService decisionService = new DecisionService(BucketerMock.Object, ErrorHandlerMock.Object, ProjectConfig, UserProfileServiceMock.Object, LoggerMock.Object);

            decisionService.GetVariation(experiment, GenericUserId, new UserAttributes());

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, string.Format("User \"{0}\" does not meet conditions to be in experiment \"{1}\".",
                GenericUserId, experiment.Key)), Times.Once);

            // ensure that a user with a saved user profile, sees the same variation regardless of audience evaluation
            decisionService.GetVariation(experiment, UserProfileId, new UserAttributes());

            BucketerMock.Verify(_ => _.Bucket(It.IsAny<ProjectConfig>(), It.IsAny<Experiment>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void TestGetForcedVariationReturnsForcedVariation()
        {
            DecisionService decisionService = new DecisionService(BucketerMock.Object, ErrorHandlerMock.Object, ProjectConfig, null, LoggerMock.Object);

            Assert.IsTrue(TestData.CompareObjects(WhitelistedVariation, decisionService.GetWhitelistedVariation(WhitelistedExperiment, WhitelistedUserId)));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, string.Format("User \"{0}\" is forced in variation \"{1}\".",
                WhitelistedUserId, WhitelistedVariation.Key)), Times.Once);

            BucketerMock.Verify(_ => _.Bucket(It.IsAny<ProjectConfig>(), It.IsAny<Experiment>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void TestGetForcedVariationWithInvalidVariation()
        {
            string userId = "testUser1";
            string invalidVariationKey = "invalidVarKey";

            DecisionService decisionService = new DecisionService(BucketerMock.Object, ErrorHandlerMock.Object, ProjectConfig, null, LoggerMock.Object);

            var variations = new Variation[]
            {
                new Variation {Id = "1", Key = "var1" }
            };
            var trafficAllocation = new TrafficAllocation[]
            {
                new TrafficAllocation {EntityId = "1", EndOfRange = 1000 }
            };

            var userIdToVariationKeyMap = new Dictionary<string, string>
            {
                {userId, invalidVariationKey }
            };

            var experiment = new Experiment
            {
                Id = "1234",
                Key = "exp_key",
                Status = "Running",
                LayerId = "1",
                AudienceIds = new string[0],
                Variations = variations,
                TrafficAllocation = trafficAllocation,
                ForcedVariations = userIdToVariationKeyMap
            };

            Assert.IsNull(decisionService.GetWhitelistedVariation(experiment, userId));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR,
                string.Format("Variation \"{0}\" is not in the datafile. Not activating user \"{1}\".", invalidVariationKey, userId)),
                Times.Once);

            BucketerMock.Verify(_ => _.Bucket(It.IsAny<ProjectConfig>(), It.IsAny<Experiment>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void TestGetForcedVariationReturnsNullWhenUserIsNotWhitelisted()
        {
            Bucketer bucketer = new Bucketer(LoggerMock.Object);
            DecisionService decisionService = new DecisionService(bucketer, ErrorHandlerMock.Object, ProjectConfig, null, LoggerMock.Object);

            Assert.IsNull(decisionService.GetWhitelistedVariation(WhitelistedExperiment, GenericUserId));
        }

        [Test]
        public void TestBucketReturnsVariationStoredInUserProfile()
        {
            Experiment experiment = ProjectConfig.Experiments[6];
            Variation variation = experiment.Variations[0];
            Decision decision = new Decision(variation.Id);

            UserProfile userProfile = new UserProfile(UserProfileId, new Dictionary<string, Decision>
            {
                { experiment.Id, decision }
            });

            UserProfileServiceMock.Setup(_ => _.Lookup(UserProfileId)).Returns(userProfile.ToMap());


            DecisionService decisionService = new DecisionService(BucketerMock.Object, ErrorHandlerMock.Object, ProjectConfig, UserProfileServiceMock.Object, LoggerMock.Object);

            Assert.IsTrue(TestData.CompareObjects(variation, decisionService.GetVariation(experiment, UserProfileId, new UserAttributes())));

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, string.Format("Returning previously activated variation \"{0}\" of experiment \"{1}\" for user \"{2}\" from user profile.",
                variation.Key, experiment.Key, UserProfileId)));

            //BucketerMock.Verify(_ => _.Bucket(It.IsAny<ProjectConfig>(), It.IsAny<Experiment>(), It.IsAny<string>()), Times.Once);

        }

        [Test]
        public void TestGetStoredVariationLogsWhenLookupReturnsNull()
        {
            Experiment experiment = ProjectConfig.Experiments[6];

            UserProfileService userProfileService = UserProfileServiceMock.Object;
            UserProfile userProfile = new UserProfile(UserProfileId, new Dictionary<string, Decision>());

            Bucketer bucketer = new Bucketer(LoggerMock.Object);
            UserProfileServiceMock.Setup(_ => _.Lookup(UserProfileId)).Returns(userProfile.ToMap());

            DecisionService decisionService = new DecisionService(bucketer,
                 ErrorHandlerMock.Object, ProjectConfig, userProfileService, LoggerMock.Object);

            Assert.IsNull(decisionService.GetStoredVariation(experiment, userProfile));

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, string.Format("No previously activated variation of experiment \"{0}\" for user \"{1}\" found in user profile."
                , experiment.Key, UserProfileId)), Times.Once);
        }

        [Test]
        public void TestGetStoredVariationReturnsNullWhenVariationIsNoLongerInConfig()
        {
            Experiment experiment = ProjectConfig.Experiments[6];
            string storedVariationId = "missingVariation";
            Decision storedDecision = new Decision(storedVariationId);

            var storedDecisions = new Dictionary<string, Decision>();

            storedDecisions[experiment.Id] = storedDecision;

            UserProfile storedUserProfile = new UserProfile(UserProfileId, storedDecisions);

            Bucketer bucketer = new Bucketer(LoggerMock.Object);

            UserProfileServiceMock.Setup(up => up.Lookup(UserProfileId)).Returns(storedUserProfile.ToMap());

            DecisionService decisionService = new DecisionService(bucketer, ErrorHandlerMock.Object, ProjectConfig,
                UserProfileServiceMock.Object, LoggerMock.Object);
            Assert.IsNull(decisionService.GetStoredVariation(experiment, storedUserProfile));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, string.Format("User \"{0}\" was previously bucketed into variation with ID \"{1}\" for experiment \"{2}\", but no matching variation was found for that user. We will re-bucket the user."
                , UserProfileId, storedVariationId, experiment.Id)), Times.Once);
        }

        [Test]
        public void TestGetVariationSavesBucketedVariationIntoUserProfile()
        {
            Experiment experiment = ProjectConfig.Experiments[6];
            Variation variation = experiment.Variations[0];

            Decision decision = new Decision(variation.Id);

            UserProfile originalUserProfile = new UserProfile(UserProfileId,
                new Dictionary<string, Decision>());
            UserProfileServiceMock.Setup(ups => ups.Lookup(UserProfileId)).Returns(originalUserProfile.ToMap());

            UserProfile expectedUserProfile = new UserProfile(UserProfileId,
                new Dictionary<string, Decision>
                {
                    {experiment.Id, decision }
                });

            var mockBucketer = new Mock<Bucketer>(LoggerMock.Object);
            mockBucketer.Setup(m => m.Bucket(ProjectConfig, experiment, UserProfileId, UserProfileId)).Returns(variation);

            DecisionService decisionService = new DecisionService(mockBucketer.Object, ErrorHandlerMock.Object, ProjectConfig, UserProfileServiceMock.Object, LoggerMock.Object);

            Assert.IsTrue(TestData.CompareObjects(variation, decisionService.GetVariation(experiment, UserProfileId, new UserAttributes())));

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, string.Format("Saved variation \"{0}\" of experiment \"{1}\" for user \"{2}\".", variation.Id,
                        experiment.Id, UserProfileId)), Times.Once);
            UserProfileServiceMock.Verify(_ => _.Save(It.IsAny<Dictionary<string, object>>()), Times.Once);
        }

        [Test]
        public void TestBucketLogsCorrectlyWhenUserProfileFailsToSave()
        {
            Experiment experiment = ProjectConfig.Experiments[6];
            Variation variation = experiment.Variations[0];
            Decision decision = new Decision(variation.Id);
            Bucketer bucketer = new Bucketer(LoggerMock.Object);

            UserProfileServiceMock.Setup(up => up.Save(It.IsAny<Dictionary<string, object>>())).Throws(new System.Exception());

            var experimentBucketMap = new Dictionary<string, Decision>();

            experimentBucketMap[experiment.Id] = decision;

            UserProfile expectedUserProfile = new UserProfile(UserProfileId, experimentBucketMap);
            UserProfile saveUserProfile = new UserProfile(UserProfileId, new Dictionary<string, Decision>());

            DecisionService decisionService = new DecisionService(bucketer,
                ErrorHandlerMock.Object, ProjectConfig, UserProfileServiceMock.Object, LoggerMock.Object);

            decisionService.SaveVariation(experiment, variation, saveUserProfile);

            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, string.Format
                ("Failed to save variation \"{0}\" of experiment \"{1}\" for user \"{2}\".", variation.Id, experiment.Id, UserProfileId))
                , Times.Once);
            ErrorHandlerMock.Verify(er => er.HandleError(It.IsAny<OptimizelySDK.Exceptions.OptimizelyRuntimeException>()), Times.Once);
        }

        [Test]
        public void TestGetVariationSavesANewUserProfile()
        {
            Experiment experiment = ProjectConfig.Experiments[6];
            Variation variation = experiment.Variations[0];
            Decision decision = new Decision(variation.Id);

            UserProfile expectedUserProfile = new UserProfile(UserProfileId, new Dictionary<string, Decision>
            {
                { experiment.Id, decision }
            });

            var mockBucketer = new Mock<Bucketer>(LoggerMock.Object);
            mockBucketer.Setup(m => m.Bucket(ProjectConfig, experiment, UserProfileId, UserProfileId)).Returns(variation);

            Dictionary<string, object> userProfile = null;

            UserProfileServiceMock.Setup(up => up.Lookup(UserProfileId)).Returns(userProfile);

            DecisionService decisionService = new DecisionService(mockBucketer.Object, ErrorHandlerMock.Object, ProjectConfig,
                UserProfileServiceMock.Object, LoggerMock.Object);

            Assert.IsTrue(TestData.CompareObjects(variation, decisionService.GetVariation(experiment, UserProfileId, new UserAttributes())));
            UserProfileServiceMock.Verify(_ => _.Save(It.IsAny<Dictionary<string, object>>()), Times.Once);
        }

        [Test]
        public void TestGetVariationUserWithSetForcedVariation()
        {
            var experimentKey = "test_experiment";
            var pausedExperimentKey = "paused_experiment";
            var userId = "test_user";
            var expectedForcedVariationKey = "variation";
            var optlyObject = new Optimizely(TestData.Datafile, new ValidEventDispatcher(), LoggerMock.Object);

            var userAttributes = new UserAttributes
            {
                {"device_type", "iPhone" },
                {"location", "San Francisco" }
            };

            optlyObject.Activate(experimentKey, userId, userAttributes);

            // confirm normal bucketing occurs before setting the forced variation
            var actualVariation = optlyObject.GetVariation(experimentKey, userId, userAttributes);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, actualVariation));

            // test valid experiment
            Assert.IsTrue(optlyObject.SetForcedVariation(experimentKey, userId, expectedForcedVariationKey), string.Format(@"Set variation to ""{0}"" failed.", expectedForcedVariationKey));

            var actualForcedVariation = optlyObject.GetVariation(experimentKey, userId, userAttributes);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyVariation, actualForcedVariation));

            // clear forced variation and confirm that normal bucketing occurs
            Assert.IsTrue(optlyObject.SetForcedVariation(experimentKey, userId, null));

            actualVariation = optlyObject.GetVariation(experimentKey, userId, userAttributes);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, actualVariation));

            // check that a paused experiment returns null
            Assert.IsTrue(optlyObject.SetForcedVariation(pausedExperimentKey, userId, expectedForcedVariationKey), string.Format(@"Set variation to ""{0}"" failed.", expectedForcedVariationKey));
            actualForcedVariation = optlyObject.GetVariation(pausedExperimentKey, userId, userAttributes);

            Assert.IsNull(actualForcedVariation);
        }

        [Test]
        public void TestGetVariationWithBucketingId()
        {
            var pausedExperimentKey = "paused_experiment";
            var userId = "test_user";
            var testUserIdWhitelisted = "user1";
            var experimentKey = "test_experiment";
            var testBucketingIdControl = "testBucketingIdControl!";  // generates bucketing number 3741
            var testBucketingIdVariation = "123456789"; // generates bucketing number 4567
            var variationKeyControl = "control";

            var testUserAttributes = new UserAttributes
            {
                {"device_type", "iPhone" },
                {"company", "Optimizely" },
                {"location", "San Francisco" }
            };

            var userAttributesWithBucketingId = new UserAttributes
            {
                {"device_type", "iPhone"},
                {"company", "Optimizely"},
                {"location", "San Francisco"},
                {ControlAttributes.BUCKETING_ID_ATTRIBUTE, testBucketingIdVariation}
            };

            var userAttributesWithInvalidBucketingId = new UserAttributes
            {
                {"device_type", "iPhone"},
                {"company", "Optimizely"},
                {"location", "San Francisco"},
                {ControlAttributes.BUCKETING_ID_ATTRIBUTE, 1.59}
            };

            var invalidUserAttributesWithBucketingId = new UserAttributes
            {
                {"company", "Optimizely"},
                {ControlAttributes.BUCKETING_ID_ATTRIBUTE, testBucketingIdControl}
            };

            var optlyObject = new Optimizely(TestData.Datafile, new ValidEventDispatcher(), LoggerMock.Object);

            // confirm normal bucketing occurs before setting the bucketing ID
            var actualVariation = optlyObject.GetVariation(experimentKey, userId, testUserAttributes);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, actualVariation));

            // confirm valid bucketing with bucketing ID set in attributes
            actualVariation = optlyObject.GetVariation(experimentKey, userId, userAttributesWithBucketingId);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyVariation, actualVariation));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, $"BucketingId is valid: \"{testBucketingIdVariation}\""));

            // check audience with invalid bucketing Id
            actualVariation = optlyObject.GetVariation(experimentKey, userId, userAttributesWithInvalidBucketingId);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, actualVariation));
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, "BucketingID attribute is not a string. Defaulted to userId"));

            // check invalid audience with bucketing ID
            actualVariation = optlyObject.GetVariation(experimentKey, userId, invalidUserAttributesWithBucketingId);
            Assert.Null(actualVariation);

            // check null audience with bucketing Id
            actualVariation = optlyObject.GetVariation(experimentKey, userId, null);
            Assert.Null(actualVariation);

            // test that an experiment that's not running returns a null variation
            actualVariation = optlyObject.GetVariation(pausedExperimentKey, userId, userAttributesWithBucketingId);
            Assert.Null(actualVariation);

            // check forced variation
            Assert.IsTrue(optlyObject.SetForcedVariation(experimentKey, userId, variationKeyControl), string.Format("Set variation to \"{0}\" failed.", variationKeyControl));
            actualVariation = optlyObject.GetVariation(experimentKey, userId, userAttributesWithBucketingId);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, actualVariation));

            // check whitelisted variation
            actualVariation = optlyObject.GetVariation(experimentKey, testUserIdWhitelisted, userAttributesWithBucketingId);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, actualVariation));

            var bucketerMock = new Mock<Bucketer>(LoggerMock.Object);
            var decision = new Decision("7722370027");
            UserProfile storedUserProfile = new UserProfile(userId, new Dictionary<string, Decision>
            {
                { "7716830082", decision }
            });

            UserProfileServiceMock.Setup(up => up.Lookup(userId)).Returns(storedUserProfile.ToMap());
            DecisionService decisionService = new DecisionService(bucketerMock.Object, ErrorHandlerMock.Object, ProjectConfig, UserProfileServiceMock.Object, LoggerMock.Object);

            actualVariation = optlyObject.GetVariation(experimentKey, userId, userAttributesWithBucketingId);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, actualVariation), string.Format("Variation \"{0}\" does not match expected user profile variation \"{1}\".", actualVariation?.Key, variationKeyControl));
        }

        #region GetVariationForFeatureExperiment Tests

        // Should return null and log a message when the feature flag's experiment ids array is empty
        [Test]
        public void TestGetVariationForFeatureExperimentGivenNullExperimentIds()
        {
            var featureFlag = ProjectConfig.GetFeatureFlagFromKey("empty_feature");
            var decision = DecisionService.GetVariationForFeatureExperiment(featureFlag, GenericUserId, new UserAttributes() { });

            Assert.IsNull(decision);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, $"The feature flag \"{featureFlag.Key}\" is not used in any experiments."));
        }

        // Should return null and log when the experiment is not in the datafile 
        [Test]
        public void TestGetVariationForFeatureExperimentGivenExperimentNotInDataFile()
        {
            var booleanFeature = ProjectConfig.GetFeatureFlagFromKey("boolean_feature");
            var featureFlag = new FeatureFlag
            {
                Id = booleanFeature.Id,
                Key = booleanFeature.Key,
                RolloutId = booleanFeature.RolloutId,
                ExperimentIds = new List<string> { "29039203" }
            };

            var decision = DecisionService.GetVariationForFeatureExperiment(featureFlag, GenericUserId, new UserAttributes() { });
            Assert.IsNull(decision);

            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Experiment ID \"29039203\" is not in datafile."));
        }

        // Should return null and log when the user is not bucketed into the feature flag's experiments
        [Test]
        public void TestGetVariationForFeatureExperimentGivenNonMutexGroupAndUserNotBucketed()
        {
            var multiVariateExp = ProjectConfig.GetExperimentFromKey("test_experiment_multivariate");

            DecisionServiceMock.Setup(ds => ds.GetVariation(multiVariateExp, "user1", null)).Returns<Variation>(null);
            var featureFlag = ProjectConfig.GetFeatureFlagFromKey("multi_variate_feature");

            var decision = DecisionServiceMock.Object.GetVariationForFeatureExperiment(featureFlag, "user1", new UserAttributes());
            Assert.IsNull(decision);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "The user \"user1\" is not bucketed into any of the experiments on the feature \"multi_variate_feature\"."));
        }

        // Should return the variation when the user is bucketed into a variation for the experiment on the feature flag
        [Test]
        public void TestGetVariationForFeatureExperimentGivenNonMutexGroupAndUserIsBucketed()
        {
            var experiment = ProjectConfig.GetExperimentFromKey("test_experiment_multivariate");
            var variation = ProjectConfig.GetVariationFromId("test_experiment_multivariate", "122231");
            var expectedDecision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);
            var userAttributes = new UserAttributes();

            DecisionServiceMock.Setup(ds => ds.GetVariation(ProjectConfig.GetExperimentFromKey("test_experiment_multivariate"), 
                "user1", userAttributes)).Returns(variation);

            var featureFlag = ProjectConfig.GetFeatureFlagFromKey("multi_variate_feature");
            var decision = DecisionServiceMock.Object.GetVariationForFeatureExperiment(featureFlag, "user1", userAttributes);

            Assert.IsTrue(TestData.CompareObjects(expectedDecision, decision));

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "The user \"user1\" is bucketed into experiment \"test_experiment_multivariate\" of feature \"multi_variate_feature\"."));
        }

        // Should return the variation the user is bucketed into when the user is bucketed into one of the experiments
        [Test]
        public void TestGetVariationForFeatureExperimentGivenMutexGroupAndUserIsBucketed()
        {
            var mutexExperiment = ProjectConfig.GetExperimentFromKey("group_experiment_1");
            var variation = mutexExperiment.Variations[0];
            var userAttributes = new UserAttributes();
            var expectedDecision = new FeatureDecision(mutexExperiment, variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);

            DecisionServiceMock.Setup(ds => ds.GetVariation(ProjectConfig.GetExperimentFromKey("group_experiment_1"), "user1", 
                userAttributes)).Returns(variation);

            var featureFlag = ProjectConfig.GetFeatureFlagFromKey("boolean_feature");
            var actualDecision = DecisionServiceMock.Object.GetVariationForFeatureExperiment(featureFlag, "user1", userAttributes);

            Assert.IsTrue(TestData.CompareObjects(expectedDecision, actualDecision));

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "The user \"user1\" is bucketed into experiment \"group_experiment_1\" of feature \"boolean_feature\"."));
        }

        // Should return null and log a message when the user is not bucketed into any of the mutex experiments 
        [Test]
        public void TestGetVariationForFeatureExperimentGivenMutexGroupAndUserNotBucketed()
        {
            var mutexExperiment = ProjectConfig.GetExperimentFromKey("group_experiment_1");
            DecisionServiceMock.Setup(ds => ds.GetVariation(It.IsAny<Experiment>(), It.IsAny<string>(), It.IsAny<UserAttributes>())).
                Returns<Variation>(null);

            var featureFlag = ProjectConfig.GetFeatureFlagFromKey("boolean_feature");
            var actualDecision = DecisionServiceMock.Object.GetVariationForFeatureExperiment(featureFlag, "user1", new UserAttributes());

            Assert.IsNull(actualDecision);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "The user \"user1\" is not bucketed into any of the experiments on the feature \"boolean_feature\"."));
        }

        #endregion // GetVariationForFeatureExperiment Tests

        #region GetVariationForFeatureRollout Tests

        // Should return null when rollout doesn't exist for the feature.
        [Test]
        public void TestGetVariationForFeatureRolloutWhenRolloutIsNotInDataFile()
        {
            var featureFlag = ProjectConfig.GetFeatureFlagFromKey("boolean_feature");
            var invalidRolloutFeature = new FeatureFlag {
                RolloutId = "invalid_rollout_id",
                Id = featureFlag.Id,
                Key = featureFlag.Key,
                ExperimentIds = new List<string>(featureFlag.ExperimentIds),
                Variables = featureFlag.Variables
                };

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeatureExperiment(It.IsAny<FeatureFlag>(), It.IsAny<string>(), It.IsAny<UserAttributes>())).Returns<Variation>(null);

            var actualDecision = DecisionServiceMock.Object.GetVariationForFeatureRollout(featureFlag, "user1", new UserAttributes());
            Assert.IsNull(actualDecision);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "The feature flag \"boolean_feature\" is not used in a rollout."));
        }
        
        // Should return the variation the user is bucketed into when the user is bucketed into the targeting rule
        [Test]
        public void TestGetVariationForFeatureRolloutWhenUserIsBucketedInTheTargetingRule()
        {
            var featureFlag = ProjectConfig.GetFeatureFlagFromKey("boolean_single_variable_feature");
            var rolloutId = featureFlag.RolloutId;
            var rollout = ProjectConfig.GetRolloutFromId(rolloutId);
            var experiment = rollout.Experiments[0];
            var variation = experiment.Variations[0];
            var expectedDecision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT);

            var userAttributes = new UserAttributes {
                { "browser_type", "chrome" }
            };

            BucketerMock.Setup(bm => bm.Bucket(It.IsAny<ProjectConfig>(), It.IsAny<Experiment>(), It.IsAny<string>(), 
                It.IsAny<string>())).Returns(variation);
            var decisionService = new DecisionService(BucketerMock.Object, ErrorHandlerMock.Object, ProjectConfig, null, LoggerMock.Object);

            var actualDecision = decisionService.GetVariationForFeatureRollout(featureFlag, "user_1", userAttributes);
            Assert.IsTrue(TestData.CompareObjects(expectedDecision, actualDecision));
        }

        // Should return the variation the user is bucketed into when the user is bucketed into the "Everyone Else" rule
        // and the user is not bucketed into the targeting rule
        [Test]
        public void TestGetVariationForFeatureRolloutWhenUserIsNotBucketedInTheTargetingRuleButBucketedToEveryoneElseRule()
        {
            var featureFlag = ProjectConfig.GetFeatureFlagFromKey("boolean_single_variable_feature");
            var rolloutId = featureFlag.RolloutId;
            var rollout = ProjectConfig.GetRolloutFromId(rolloutId);
            var experiment = rollout.Experiments[0];
            var everyoneElseRule = rollout.Experiments[rollout.Experiments.Count - 1];
            var variation = everyoneElseRule.Variations[0];
            var expectedDecision = new FeatureDecision(everyoneElseRule, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT);

            var userAttributes = new UserAttributes {
                { "browser_type", "chrome" }
            };

            BucketerMock.Setup(bm => bm.Bucket(It.IsAny<ProjectConfig>(), experiment, It.IsAny<string>(), It.IsAny<string>())).Returns<Variation>(null);
            BucketerMock.Setup(bm => bm.Bucket(It.IsAny<ProjectConfig>(), everyoneElseRule, It.IsAny<string>(), It.IsAny<string>())).Returns(variation);
            var decisionService = new DecisionService(BucketerMock.Object, ErrorHandlerMock.Object, ProjectConfig, null, LoggerMock.Object);

            var actualDecision = decisionService.GetVariationForFeatureRollout(featureFlag, "user_1", userAttributes);
            Assert.IsTrue(TestData.CompareObjects(expectedDecision, actualDecision));
        }

        // Should log and return null when  the user is not bucketed into the targeting rule 
        // as well as "Everyone Else" rule.
        [Test]
        public void TestGetVariationForFeatureRolloutWhenUserIsNeitherBucketedInTheTargetingRuleNorToEveryoneElseRule()
        {
            var featureFlag = ProjectConfig.GetFeatureFlagFromKey("boolean_single_variable_feature");
            var rolloutId = featureFlag.RolloutId;
            var rollout = ProjectConfig.GetRolloutFromId(rolloutId);
            var userAttributes = new UserAttributes {
                { "browser_type", "chrome" }
            };

            BucketerMock.Setup(bm => bm.Bucket(It.IsAny<ProjectConfig>(), It.IsAny<Experiment>(), It.IsAny<string>(), It.IsAny<string>())).Returns<Variation>(null);
            var decisionService = new DecisionService(BucketerMock.Object, ErrorHandlerMock.Object, ProjectConfig, null, LoggerMock.Object);

            var actualDecision = decisionService.GetVariationForFeatureRollout(featureFlag, "user_1", userAttributes);
            Assert.IsNull(actualDecision);
        }
        
        // Should return expected variation when the user is attempted to be bucketed into all targeting rules
        // including Everyone Else rule
        [Test]
        public void TestGetVariationForFeatureRolloutWhenUserDoesNotQualifyForAnyTargetingRule()
        {
            var featureFlag = ProjectConfig.GetFeatureFlagFromKey("boolean_single_variable_feature");
            var rolloutId = featureFlag.RolloutId;
            var rollout = ProjectConfig.GetRolloutFromId(rolloutId);
            var experiment0 = rollout.Experiments[0];
            var experiment1 = rollout.Experiments[1];
            var everyoneElseRule = rollout.Experiments[rollout.Experiments.Count - 1];
            var variation = everyoneElseRule.Variations[0];
            var expectedDecision = new FeatureDecision(everyoneElseRule, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT);

            BucketerMock.Setup(bm => bm.Bucket(It.IsAny<ProjectConfig>(), everyoneElseRule, It.IsAny<string>(), It.IsAny<string>())).Returns(variation);
            var decisionService = new DecisionService(BucketerMock.Object, ErrorHandlerMock.Object, ProjectConfig, null, LoggerMock.Object);

            // Provide null attributes so that user does not qualify for audience.
            var actualDecision = decisionService.GetVariationForFeatureRollout(featureFlag, "user_1", null);

            Assert.IsTrue(TestData.CompareObjects(expectedDecision, actualDecision));

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, $"User \"user_1\" does not meet the conditions to be in rollout rule for audience \"{ProjectConfig.AudienceIdMap[experiment0.AudienceIds[0]].Name}\"."));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, $"User \"user_1\" does not meet the conditions to be in rollout rule for audience \"{ProjectConfig.AudienceIdMap[experiment1.AudienceIds[0]].Name}\"."));
        }
        
        [Test]
        public void TestGetVariationForFeatureRolloutAudienceAndTrafficeAllocationCheck()
        {
            var featureFlag = ProjectConfig.GetFeatureFlagFromKey("boolean_single_variable_feature");
            var rolloutId = featureFlag.RolloutId;
            var rollout = ProjectConfig.GetRolloutFromId(rolloutId);
            var expWithAudienceiPhoneUsers = rollout.Experiments[1];
            var expWithAudienceChromeUsers = rollout.Experiments[0];
            var expWithNoAudience = rollout.Experiments[2];
            var varWithAudienceiPhoneUsers = expWithAudienceiPhoneUsers.Variations[0];
            var varWithAudienceChromeUsers = expWithAudienceChromeUsers.Variations[0];
            var varWithNoAudience = expWithNoAudience.Variations[0];

            var mockBucketer = new Mock<Bucketer>(LoggerMock.Object) { CallBase = true };
            mockBucketer.Setup(bm => bm.GenerateBucketValue(It.IsAny<string>())).Returns(980);
            var decisionService = new DecisionService(mockBucketer.Object, ErrorHandlerMock.Object, ProjectConfig, null, LoggerMock.Object);

            // Calling with audience iPhone users in San Francisco.
            var actualDecision = decisionService.GetVariationForFeatureRollout(featureFlag, GenericUserId, new UserAttributes
            {
                { "device_type", "iPhone" },
                { "location", "San Francisco" }
            });

            // Returned variation id should be '177773' because of audience 'iPhone users in San Francisco'.
            var expectedDecision = new FeatureDecision(expWithAudienceiPhoneUsers, varWithAudienceiPhoneUsers, FeatureDecision.DECISION_SOURCE_ROLLOUT);
            Assert.IsTrue(TestData.CompareObjects(expectedDecision, actualDecision));

            // Calling with audience Chrome users.
            actualDecision = decisionService.GetVariationForFeatureRollout(featureFlag, GenericUserId, new UserAttributes
            {
                { "browser_type", "chrome" }
            });

            // Returned variation id should be '177771' because of audience 'Chrome users'.
            expectedDecision = new FeatureDecision(expWithAudienceChromeUsers, varWithAudienceChromeUsers, FeatureDecision.DECISION_SOURCE_ROLLOUT);
            Assert.IsTrue(TestData.CompareObjects(expectedDecision, actualDecision));

            // Calling with no audience.
            mockBucketer.Setup(bm => bm.GenerateBucketValue(It.IsAny<string>())).Returns(8000);
            actualDecision = decisionService.GetVariationForFeatureRollout(featureFlag, GenericUserId, new UserAttributes());

            // Returned variation id should be of everyone else rule because of no audience.
            expectedDecision = new FeatureDecision(expWithNoAudience, varWithNoAudience, FeatureDecision.DECISION_SOURCE_ROLLOUT);
            Assert.IsTrue(TestData.CompareObjects(expectedDecision, actualDecision));

            // Calling with audience 'Chrome users' and traffice allocation '9500'.
            mockBucketer.Setup(bm => bm.GenerateBucketValue(It.IsAny<string>())).Returns(9500);
            actualDecision = decisionService.GetVariationForFeatureRollout(featureFlag, GenericUserId, new UserAttributes
            {
                { "browser_type", "chrome" }
            });

            // Returned decision entity should be null because bucket value exceeds traffice allocation of everyone else rule.
            Assert.Null(actualDecision);
        }

        [Test]
        public void TestGetVariationForFeatureRolloutCheckAudienceInEveryoneElseRule()
        {
            var featureFlag = ProjectConfig.GetFeatureFlagFromKey("boolean_single_variable_feature");
            var rolloutId = featureFlag.RolloutId;
            var rollout = ProjectConfig.GetRolloutFromId(rolloutId);
            var everyoneElseRule = rollout.Experiments[2];
            var variation = everyoneElseRule.Variations[0];
            var expectedDecision = new FeatureDecision(everyoneElseRule, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT);

            BucketerMock.Setup(bm => bm.Bucket(It.IsAny<ProjectConfig>(), everyoneElseRule, It.IsAny<string>(), WhitelistedUserId)).Returns(variation);
            BucketerMock.Setup(bm => bm.Bucket(It.IsAny<ProjectConfig>(), everyoneElseRule, It.IsAny<string>(), GenericUserId)).Returns<Variation>(null);
            var decisionService = new DecisionService(BucketerMock.Object, ErrorHandlerMock.Object, ProjectConfig, null, LoggerMock.Object);

            // Returned variation id should be of everyone else rule as it passes audience Id checking.
            var actualDecision = decisionService.GetVariationForFeatureRollout(featureFlag, WhitelistedUserId, null);
            Assert.True(TestData.CompareObjects(expectedDecision, actualDecision));

            // Returned variation id should be null.
            actualDecision = decisionService.GetVariationForFeatureRollout(featureFlag, GenericUserId, null);
            Assert.Null(actualDecision);

            // Returned variation id should be null as it fails audience Id checking.
            everyoneElseRule.AudienceIds = new string[] { ProjectConfig.Audiences[0].Id };
            actualDecision = decisionService.GetVariationForFeatureRollout(featureFlag, GenericUserId, null);
            Assert.Null(actualDecision);

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "User \"testUser1\" does not meet the conditions to be in rollout rule for audience \"Chrome users\"."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "User \"testUser1\" does not meet the conditions to be in rollout rule for audience \"iPhone users in San Francisco\"."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "User \"genericUserId\" does not meet the conditions to be in rollout rule for audience \"Chrome users\"."), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "User \"genericUserId\" does not meet the conditions to be in rollout rule for audience \"iPhone users in San Francisco\"."), Times.Exactly(3));
        }

        #endregion // GetVariationForFeatureRollout Tests

        #region GetVariationForFeature Tests

        // Should return the variation the user was bucketed into when the user is in the feature flag's experiment.
        [Test]
        public void TestGetVariationForFeatureWhenTheUserIsBucketedIntoFeatureExperiment()
        {
            var featureFlag = ProjectConfig.GetFeatureFlagFromKey("string_single_variable_feature");
            var expectedExperimentId = featureFlag.ExperimentIds[0];
            var expectedExperiment = ProjectConfig.GetExperimentFromId(expectedExperimentId);
            var variation = expectedExperiment.Variations[0];
            var expectedDecision = new FeatureDecision(expectedExperiment, variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeatureExperiment(It.IsAny<FeatureFlag>(), It.IsAny<string>(), 
                It.IsAny<UserAttributes>())).Returns(expectedDecision);

            var actualDecision = DecisionServiceMock.Object.GetVariationForFeature(featureFlag, "user1", new UserAttributes());
            Assert.IsTrue(TestData.CompareObjects(expectedDecision, actualDecision));
        }

        // Should return the bucketed variation when the user is not bucketed in the feature flag experiment, 
        // but is bucketed into a variation of the feature flag's rollout.
        [Test]
        public void TestGetVariationForFeatureWhenTheUserIsNotBucketedIntoFeatureExperimentAndBucketedToFeatureRollout()
        {
            var featureFlag = ProjectConfig.GetFeatureFlagFromKey("string_single_variable_feature");
            var rolloutId = featureFlag.RolloutId;
            var rollout = ProjectConfig.GetRolloutFromId(rolloutId);
            var expectedExperiment = rollout.Experiments[0];
            var variation = expectedExperiment.Variations[0];
            var expectedDecision = new FeatureDecision(expectedExperiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeatureExperiment(It.IsAny<FeatureFlag>(), It.IsAny<string>(), 
                It.IsAny<UserAttributes>())).Returns<Variation>(null);
            DecisionServiceMock.Setup(ds => ds.GetVariationForFeatureRollout(It.IsAny<FeatureFlag>(), It.IsAny<string>(), 
                It.IsAny<UserAttributes>())).Returns(expectedDecision);

            var actualDecision = DecisionServiceMock.Object.GetVariationForFeature(featureFlag, "user1", new UserAttributes());

            Assert.IsTrue(TestData.CompareObjects(expectedDecision, actualDecision));

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "The user \"user1\" is bucketed into a rollout for feature flag \"string_single_variable_feature\"."));
        }

        // Should return null when the user neither gets bucketed into feature experiment nor in feature rollout.
        [Test]
        public void TestGetVariationForFeatureWhenTheUserIsNeitherBucketedIntoFeatureExperimentNorToFeatureRollout()
        {
            var featureFlag = ProjectConfig.GetFeatureFlagFromKey("string_single_variable_feature");
            var expectedDecision = new FeatureDecision(null, null, FeatureDecision.DECISION_SOURCE_ROLLOUT);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeatureExperiment(It.IsAny<FeatureFlag>(), It.IsAny<string>(), It.IsAny<UserAttributes>())).Returns<Variation>(null);
            DecisionServiceMock.Setup(ds => ds.GetVariationForFeatureRollout(It.IsAny<FeatureFlag>(), It.IsAny<string>(), It.IsAny<UserAttributes>())).Returns<Variation>(null);

            var actualDecision = DecisionServiceMock.Object.GetVariationForFeature(featureFlag, "user1", new UserAttributes());
            Assert.IsTrue(TestData.CompareObjects(expectedDecision, actualDecision));

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "The user \"user1\" is not bucketed into a rollout for feature flag \"string_single_variable_feature\"."));
        }

        // Verify that the user is bucketed into the experiment's variation when the user satisfies bucketing and traffic allocation 
        // for feature flag experiment and feature flag rollout.
        [Test]
        public void TestGetVariationForFeatureWhenTheUserIsBuckedtedInBothExperimentAndRollout()
        {
            var featureFlag = ProjectConfig.GetFeatureFlagFromKey("string_single_variable_feature");
            var experiment = ProjectConfig.GetExperimentFromKey("test_experiment_with_feature_rollout");
            var variation = ProjectConfig.GetVariationFromId("test_experiment_with_feature_rollout", "122236");
            var expectedDecision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);
            var userAttributes = new UserAttributes {
                { "browser_type", "chrome" }
            };

            DecisionServiceMock.Setup(ds => ds.GetVariation(experiment, "user1", userAttributes)).Returns(variation);
            var actualDecision = DecisionServiceMock.Object.GetVariationForFeatureExperiment(featureFlag, "user1", userAttributes);

            // The user is bucketed into feature experiment's variation.
            Assert.IsTrue(TestData.CompareObjects(expectedDecision, actualDecision));

            var rollout = ProjectConfig.GetRolloutFromId(featureFlag.RolloutId);
            var rolloutExperiment = rollout.Experiments[0];
            var rolloutVariation = rolloutExperiment.Variations[0];
            var expectedRolloutDecision = new FeatureDecision(rolloutExperiment, rolloutVariation, FeatureDecision.DECISION_SOURCE_ROLLOUT);

            BucketerMock.Setup(bm => bm.Bucket(ProjectConfig, rolloutExperiment, It.IsAny<string>(), It.IsAny<string>())).Returns(rolloutVariation);
            var actualRolloutDecision = DecisionServiceMock.Object.GetVariationForFeatureRollout(featureFlag, "user1", userAttributes);

            // The user is bucketed into feature rollout's variation.

            Assert.IsTrue(TestData.CompareObjects(expectedRolloutDecision, actualRolloutDecision));

            actualDecision = DecisionServiceMock.Object.GetVariationForFeature(featureFlag, "user1", userAttributes);

            // The user is bucketed into feature experiment's variation and not the rollout's variation.
            Assert.IsTrue(TestData.CompareObjects(expectedDecision, actualDecision));

        }

        #endregion // GetVariationForFeature Tests
    }
}