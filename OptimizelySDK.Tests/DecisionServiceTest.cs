/**
 *
 *    Copyright 2017, Optimizely and contributors
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

        private ProjectConfig NoAudienceProjectConfig;
        private ProjectConfig ValidProjectConfig;
        private ProjectConfig ProjectConfig;
        private Experiment WhitelistedExperiment;
        private Variation WhitelistedVariation;
        private DecisionService DecisionService;

        [SetUp]
        public void SetUp()
        {
            LoggerMock              = new Mock<ILogger>();
            ErrorHandlerMock        = new Mock<IErrorHandler>();
            UserProfileServiceMock  = new Mock<UserProfileService>();
            BucketerMock            = new Mock<Bucketer>(LoggerMock.Object);
            
            ValidProjectConfig      = ProjectConfig.Create(TestData.ValidDataFileV3, LoggerMock.Object, ErrorHandlerMock.Object);
            NoAudienceProjectConfig = ProjectConfig.Create(TestData.NoAudienceProjectConfigV3, LoggerMock.Object, ErrorHandlerMock.Object);
            WhitelistedExperiment   = ValidProjectConfig.ExperimentIdMap["223"];
            WhitelistedVariation    = WhitelistedExperiment.VariationKeyToVariationMap["vtag1"];
            ProjectConfig           = ProjectConfig.Create(TestData.Datafile, LoggerMock.Object, ErrorHandlerMock.Object);
            DecisionService         = new DecisionService(new Bucketer(LoggerMock.Object), ErrorHandlerMock.Object, ProjectConfig, null, LoggerMock.Object);

            DecisionServiceMock     = new Mock<DecisionService>(BucketerMock.Object, ErrorHandlerMock.Object, ProjectConfig, null, LoggerMock.Object) { CallBase = true };
        }

        [Test]
        public void TestGetVariationForcedVariationPrecedesAudienceEval()
        {
            var BucketerMock = new Mock<Bucketer>(LoggerMock.Object);

            DecisionService decisionService = new DecisionService(BucketerMock.Object, ErrorHandlerMock.Object, ValidProjectConfig, null, LoggerMock.Object);
            Experiment experiment = ValidProjectConfig.Experiments[0];
            Variation expectedVariation = experiment.Variations[0];

            // user excluded without audiences and whitelisting
            Assert.IsNull(decisionService.GetVariation(experiment, GenericUserId, new UserAttributes()));

            var actualVariation = decisionService.GetVariation(experiment, WhitelistedUserId, new UserAttributes());

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, string.Format("User \"{0}\" is forced in variation \"vtag1\".", WhitelistedUserId)), Times.Once);
            // no attributes provided for a experiment that has an audience
            Assert.IsTrue(TestData.CompareObjects(actualVariation, expectedVariation));
            BucketerMock.Verify(_ => _.Bucket(It.IsAny<ProjectConfig>(), It.IsAny<Experiment>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void TestGetVariationEvaluatesUserProfileBeforeAudienceTargeting()
        {
            var BucketerMock = new Mock<Bucketer>(LoggerMock.Object);
            Experiment experiment = ValidProjectConfig.Experiments[0];
            Variation variation = experiment.Variations[0];

            Decision decision = new Decision(variation.Id);
            UserProfile userProfile = new UserProfile(UserProfileId, new Dictionary<string, Decision>
            {
                { experiment.Id, decision }
            });

            UserProfileServiceMock.Setup(up => up.Lookup(WhitelistedUserId)).Returns(userProfile.ToMap());

            DecisionService decisionService = new DecisionService(BucketerMock.Object, ErrorHandlerMock.Object, ValidProjectConfig, UserProfileServiceMock.Object, LoggerMock.Object);

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
            var BucketerMock = new Mock<Bucketer>(LoggerMock.Object);

            DecisionService decisionService = new DecisionService(BucketerMock.Object, ErrorHandlerMock.Object, ValidProjectConfig, null, LoggerMock.Object);

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

            var BucketerMock = new Mock<Bucketer>(LoggerMock.Object);
            DecisionService decisionService = new DecisionService(BucketerMock.Object, ErrorHandlerMock.Object, ValidProjectConfig, null, LoggerMock.Object);

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
            DecisionService decisionService = new DecisionService(bucketer, ErrorHandlerMock.Object, ValidProjectConfig, null, LoggerMock.Object);

            Assert.IsNull(decisionService.GetWhitelistedVariation(WhitelistedExperiment, GenericUserId));
        }

        [Test]
        public void TestBucketReturnsVariationStoredInUserProfile()
        {
            Experiment experiment = NoAudienceProjectConfig.Experiments[0];
            Variation variation = experiment.Variations[0];
            Decision decision = new Decision(variation.Id);
            var BucketerMock = new Mock<Bucketer>(LoggerMock.Object);

            UserProfile userProfile = new UserProfile(UserProfileId, new Dictionary<string, Decision>
            {
                { experiment.Id, decision }
            });

            UserProfileServiceMock.Setup(_ => _.Lookup(UserProfileId)).Returns(userProfile.ToMap());


            DecisionService decisionService = new DecisionService(BucketerMock.Object, ErrorHandlerMock.Object, NoAudienceProjectConfig, UserProfileServiceMock.Object, LoggerMock.Object);

            Assert.IsTrue(TestData.CompareObjects(variation, decisionService.GetVariation(experiment, UserProfileId, new UserAttributes())));

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, string.Format("Returning previously activated variation \"{0}\" of experiment \"{1}\" for user \"{2}\" from user profile.",
                variation.Key, experiment.Key, UserProfileId)));

            //BucketerMock.Verify(_ => _.Bucket(It.IsAny<ProjectConfig>(), It.IsAny<Experiment>(), It.IsAny<string>()), Times.Once);

        }

        [Test]
        public void TestGetStoredVariationLogsWhenLookupReturnsNull()
        {
            Experiment experiment = NoAudienceProjectConfig.Experiments[0];

            UserProfileService userProfileService = UserProfileServiceMock.Object;
            UserProfile userProfile = new UserProfile(UserProfileId, new Dictionary<string, Decision>());

            Bucketer bucketer = new Bucketer(LoggerMock.Object);
            UserProfileServiceMock.Setup(_ => _.Lookup(UserProfileId)).Returns(userProfile.ToMap());

            DecisionService decisionService = new DecisionService(bucketer,
                 ErrorHandlerMock.Object, NoAudienceProjectConfig, userProfileService, LoggerMock.Object);

            Assert.IsNull(decisionService.GetStoredVariation(experiment, userProfile));

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, string.Format("No previously activated variation of experiment \"{0}\" for user \"{1}\" found in user profile."
                , experiment.Key, UserProfileId)), Times.Once);
        }

        [Test]
        public void TestGetStoredVariationReturnsNullWhenVariationIsNoLongerInConfig()
        {
            Experiment experiment = NoAudienceProjectConfig.Experiments[0];
            string storedVariationId = "missingVariation";
            Decision storedDecision = new Decision(storedVariationId);

            var storedDecisions = new Dictionary<string, Decision>();

            storedDecisions[experiment.Id] = storedDecision;

            UserProfile storedUserProfile = new UserProfile(UserProfileId, storedDecisions);

            Bucketer bucketer = new Bucketer(LoggerMock.Object);

            UserProfileServiceMock.Setup(up => up.Lookup(UserProfileId)).Returns(storedUserProfile.ToMap());

            DecisionService decisionService = new DecisionService(bucketer, ErrorHandlerMock.Object, NoAudienceProjectConfig,
                UserProfileServiceMock.Object, LoggerMock.Object);
            Assert.IsNull(decisionService.GetStoredVariation(experiment, storedUserProfile));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, string.Format("User \"{0}\" was previously bucketed into variation with ID \"{1}\" for experiment \"{2}\", but no matching variation was found for that user. We will re-bucket the user."
                , UserProfileId, storedVariationId, experiment.Id)), Times.Once);
        }

        [Test]
        public void TestGetVariationSavesBucketedVariationIntoUserProfile()
        {
            Experiment experiment = NoAudienceProjectConfig.Experiments[0];
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
            mockBucketer.Setup(m => m.Bucket(ValidProjectConfig, experiment, UserProfileId, UserProfileId)).Returns(variation);

            DecisionService decisionService = new DecisionService(mockBucketer.Object, ErrorHandlerMock.Object, ValidProjectConfig, UserProfileServiceMock.Object, LoggerMock.Object);

            Assert.IsTrue(TestData.CompareObjects(variation, decisionService.GetVariation(experiment, UserProfileId, new UserAttributes())));

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, string.Format("Saved variation \"{0}\" of experiment \"{1}\" for user \"{2}\".", variation.Id,
                        experiment.Id, UserProfileId)), Times.Once);
            UserProfileServiceMock.Verify(_ => _.Save(It.IsAny<Dictionary<string, object>>()), Times.Once);
        }

        [Test]
        public void TestBucketLogsCorrectlyWhenUserProfileFailsToSave()
        {
            Experiment experiment = NoAudienceProjectConfig.Experiments[0];
            Variation variation = experiment.Variations[0];
            Decision decision = new Decision(variation.Id);
            Bucketer bucketer = new Bucketer(LoggerMock.Object);

            UserProfileServiceMock.Setup(up => up.Save(It.IsAny<Dictionary<string, object>>())).Throws(new System.Exception());

            var experimentBucketMap = new Dictionary<string, Decision>();

            experimentBucketMap[experiment.Id] = decision;

            UserProfile expectedUserProfile = new UserProfile(UserProfileId, experimentBucketMap);
            UserProfile saveUserProfile = new UserProfile(UserProfileId, new Dictionary<string, Decision>());

            DecisionService decisionService = new DecisionService(bucketer,
                ErrorHandlerMock.Object, NoAudienceProjectConfig, UserProfileServiceMock.Object, LoggerMock.Object);

            decisionService.SaveVariation(experiment, variation, saveUserProfile);

            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, string.Format
                ("Failed to save variation \"{0}\" of experiment \"{1}\" for user \"{2}\".", variation.Id, experiment.Id, UserProfileId))
                , Times.Once);
            ErrorHandlerMock.Verify(er => er.HandleError(It.IsAny<OptimizelySDK.Exceptions.OptimizelyRuntimeException>()), Times.Once);
        }

        [Test]
        public void TestGetVariationSavesANewUserProfile()
        {
            Experiment experiment = NoAudienceProjectConfig.Experiments[0];
            Variation variation = experiment.Variations[0];
            Decision decision = new Decision(variation.Id);

            UserProfile expectedUserProfile = new UserProfile(UserProfileId, new Dictionary<string, Decision>
            {
                { experiment.Id, decision }
            });

            var mockBucketer = new Mock<Bucketer>(LoggerMock.Object);
            mockBucketer.Setup(m => m.Bucket(NoAudienceProjectConfig, experiment, UserProfileId, UserProfileId)).Returns(variation);

            Dictionary<string, object> userProfile = null;

            UserProfileServiceMock.Setup(up => up.Lookup(UserProfileId)).Returns(userProfile);

            DecisionService decisionService = new DecisionService(mockBucketer.Object, ErrorHandlerMock.Object, NoAudienceProjectConfig,
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
            var expectedVariationKey = "control";
            var optlyObject = new Optimizely(TestData.Datafile, new ValidEventDispatcher(), LoggerMock.Object);

            var userAttributes = new UserAttributes
            {
                {"device_type", "iPhone" },
                {"location", "San Francisco" }
            };

            optlyObject.Activate(experimentKey, userId, userAttributes);

            // confirm normal bucketing occurs before setting the forced variation
            var actualVariationKey = optlyObject.GetVariation(experimentKey, userId, userAttributes);

            Assert.AreEqual(expectedVariationKey, actualVariationKey);

            // test valid experiment
            Assert.IsTrue(optlyObject.SetForcedVariation(experimentKey, userId, expectedForcedVariationKey), string.Format(@"Set variation to ""{0}"" failed.", expectedForcedVariationKey));

            var actualForcedVariationKey = optlyObject.GetVariation(experimentKey, userId, userAttributes);
            Assert.AreEqual(expectedForcedVariationKey, actualForcedVariationKey);

            // clear forced variation and confirm that normal bucketing occurs
            Assert.IsTrue(optlyObject.SetForcedVariation(experimentKey, userId, null));

            actualVariationKey = optlyObject.GetVariation(experimentKey, userId, userAttributes);
            Assert.AreEqual(expectedVariationKey, actualVariationKey);

            // check that a paused experiment returns null
            Assert.IsTrue(optlyObject.SetForcedVariation(pausedExperimentKey, userId, expectedForcedVariationKey), string.Format(@"Set variation to ""{0}"" failed.", expectedForcedVariationKey));
            actualForcedVariationKey = optlyObject.GetVariation(pausedExperimentKey, userId, userAttributes);

            Assert.IsNull(actualForcedVariationKey);
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
            var variationKeyVariation = "variation";

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
                {DecisionService.RESERVED_ATTRIBUTE_KEY_BUCKETING_ID, testBucketingIdVariation}
            };

            var invalidUserAttributesWithBucketingId = new UserAttributes
            {
                {"company", "Optimizely"},
                {DecisionService.RESERVED_ATTRIBUTE_KEY_BUCKETING_ID, testBucketingIdControl}
            };

            var optlyObject = new Optimizely(TestData.Datafile, new ValidEventDispatcher(), LoggerMock.Object);

            // confirm normal bucketing occurs before setting the bucketing ID
            var actualVariationKey = optlyObject.GetVariation(experimentKey, userId, testUserAttributes);
            Assert.AreEqual(variationKeyControl, actualVariationKey);

            // confirm valid bucketing with bucketing ID set in attributes
            actualVariationKey = optlyObject.GetVariation(experimentKey, userId, userAttributesWithBucketingId);
            Assert.AreEqual(variationKeyVariation, actualVariationKey);

            // check invalid audience with bucketing ID
            actualVariationKey = optlyObject.GetVariation(experimentKey, userId, invalidUserAttributesWithBucketingId);
            Assert.AreEqual(null, actualVariationKey);

            // check null audience with bucketing Id
            actualVariationKey = optlyObject.GetVariation(experimentKey, userId, null);
            Assert.AreEqual(null, actualVariationKey);

            // test that an experiment that's not running returns a null variation
            actualVariationKey = optlyObject.GetVariation(pausedExperimentKey, userId, userAttributesWithBucketingId);
            Assert.AreEqual(null, actualVariationKey);

            // check forced variation
            Assert.IsTrue(optlyObject.SetForcedVariation(experimentKey, userId, variationKeyControl), string.Format("Set variation to \"{0}\" failed.", variationKeyControl));
            actualVariationKey = optlyObject.GetVariation(experimentKey, userId, userAttributesWithBucketingId);
            Assert.AreEqual(variationKeyControl, actualVariationKey);

            // check whitelisted variation
            actualVariationKey = optlyObject.GetVariation(experimentKey, testUserIdWhitelisted, userAttributesWithBucketingId);
            Assert.AreEqual(variationKeyControl, actualVariationKey);

            var bucketerMock = new Mock<Bucketer>(LoggerMock.Object);
            var decision = new Decision("7722370027");
            UserProfile storedUserProfile = new UserProfile(userId, new Dictionary<string, Decision>
            {
                { "7716830082", decision }
            });

            UserProfileServiceMock.Setup(up => up.Lookup(userId)).Returns(storedUserProfile.ToMap());
            DecisionService decisionService = new DecisionService(bucketerMock.Object, ErrorHandlerMock.Object, ValidProjectConfig, UserProfileServiceMock.Object, LoggerMock.Object);

            actualVariationKey = optlyObject.GetVariation(experimentKey, userId, userAttributesWithBucketingId);
            Assert.AreEqual(variationKeyControl, actualVariationKey, string.Format("Variation \"{0}\" does not match expected user profile variation \"{1}\".", actualVariationKey, variationKeyControl));
        }
    }
}