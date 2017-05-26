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

        private ProjectConfig NoAudienceProjectConfig;
        private ProjectConfig ValidProjectConfig;
        private Experiment WhitelistedExperiment;
        private Variation WhitelistedVariation;

        [SetUp]
        public void SetUp()
        {
            LoggerMock              = new Mock<ILogger>();
            ErrorHandlerMock        = new Mock<IErrorHandler>();
            UserProfileServiceMock  = new Mock<UserProfileService>();

            ValidProjectConfig          = ProjectConfig.Create(TestData.ValidDataFileV3, LoggerMock.Object, ErrorHandlerMock.Object);
            NoAudienceProjectConfig     = ProjectConfig.Create(TestData.NoAudienceProjectConfigV3, LoggerMock.Object, ErrorHandlerMock.Object);
            WhitelistedExperiment       = ValidProjectConfig.ExperimentIdMap["223"];
            WhitelistedExperiment.GenerateKeyMap();
            WhitelistedVariation        = WhitelistedExperiment.VariationKeyToVariationMap["vtag1"];
        }

        [Test]
        public void GetVariationForcedVariationPrecedesAudienceEval()
        {
            Bucketer bucketer = new Bucketer(LoggerMock.Object);

            bucketer.Bucket(ValidProjectConfig, WhitelistedExperiment, WhitelistedUserId);

            DecisionService decisionService = new DecisionService(bucketer, ErrorHandlerMock.Object, ValidProjectConfig, null, LoggerMock.Object);
            Experiment experiment = ValidProjectConfig.Experiments[0];
            Variation expectedVariation = experiment.Variations[0];

            // user excluded without audiences and whitelisting
            Assert.IsNull(decisionService.GetVariation(experiment, GenericUserId, new UserAttributes()));

            var actualVariation = decisionService.GetVariation(experiment, WhitelistedUserId, new UserAttributes());

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, string.Format("User \"{0}\" is forced in variation \"vtag1\".", WhitelistedUserId)), Times.Once);
            // no attributes provided for a experiment that has an audience
            Assert.IsTrue(TestData.CompareObjects(actualVariation, expectedVariation));
            
            //verify(decisionService).getWhitelistedVariation(experiment, whitelistedUserId);
            //verify(decisionService, never()).getStoredVariation(eq(experiment), any(UserProfile.class));
        }

        [Test]
        public void GetVariationEvaluatesUserProfileBeforeAudienceTargeting()
        {
            Experiment experiment = ValidProjectConfig.Experiments[0];
            Variation variation = experiment.Variations[0];

            Bucketer bucketer = new Bucketer(LoggerMock.Object);
            Decision decision = new Decision(variation.Id);
            UserProfile userProfile = new UserProfile(UserProfileId, new Dictionary<string, Decision>
            {
                { experiment.Id, decision }
            });

            UserProfileServiceMock.Setup(up => up.Lookup(WhitelistedUserId)).Returns(userProfile.ToMap());

            DecisionService decisionService = new DecisionService(bucketer, ErrorHandlerMock.Object, ValidProjectConfig, UserProfileServiceMock.Object, LoggerMock.Object);

            decisionService.GetVariation(experiment, GenericUserId, new UserAttributes());

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, string.Format("User \"{0}\" does not meet conditions to be in experiment \"{1}\".",
                GenericUserId, experiment.Key)), Times.Once);

            // ensure that a user with a saved user profile, sees the same variation regardless of audience evaluation
            decisionService.GetVariation(experiment, UserProfileId, new UserAttributes());
        }

        [Test]
        public void GetForcedVariationReturnsForcedVariation()
        {
            Bucketer bucketer = new Bucketer(LoggerMock.Object);
            DecisionService decisionService = new DecisionService(bucketer, ErrorHandlerMock.Object, ValidProjectConfig, null, LoggerMock.Object);

            Assert.IsTrue(TestData.CompareObjects(WhitelistedVariation, decisionService.GetWhitelistedVariation(WhitelistedExperiment, WhitelistedUserId)));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, string.Format("User \"{0}\" is forced in variation \"{1}\".",
                WhitelistedUserId, WhitelistedVariation.Key)), Times.Once);

            //logbackVerifier.expectMessage(Level.INFO, "User \"" + whitelistedUserId + "\" is forced in variation \""
            //        + whitelistedVariation.getKey() + "\".");

            //assertEquals(whitelistedVariation, decisionService.getWhitelistedVariation(whitelistedExperiment, whitelistedUserId));
        }
        [Test]
        public void GetForcedVariationWithInvalidVariation()
        {
            string userId = "testUser1";
            string invalidVariationKey = "invalidVarKey";

            Bucketer bucketer = new Bucketer(LoggerMock.Object);
            DecisionService decisionService = new DecisionService(bucketer, ErrorHandlerMock.Object, ValidProjectConfig, null, LoggerMock.Object);

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
            experiment.GenerateKeyMap();

            //Experiment experiment = new Experiment("1234", "exp_key", "Running", "1", Collections.< String > emptyList(),
            //        variations, userIdToVariationKeyMap, trafficAllocations);
            Assert.IsNull(decisionService.GetWhitelistedVariation(experiment, userId));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR,
                string.Format("Variation \"{0}\" is not in the datafile. Not activating user \"{1}\".", invalidVariationKey, userId)), 
                Times.Once);
        }

        [Test]
        public void GetForcedVariationReturnsNullWhenUserIsNotWhitelisted()
        {
            Bucketer bucketer = new Bucketer(LoggerMock.Object);
            DecisionService decisionService = new DecisionService(bucketer, ErrorHandlerMock.Object, ValidProjectConfig, null, LoggerMock.Object);

            Assert.IsNull(decisionService.GetWhitelistedVariation(WhitelistedExperiment, GenericUserId));
        }

        [Test]
        public void BucketReturnsVariationStoredInUserProfile()
        {
            Experiment experiment = NoAudienceProjectConfig.Experiments[0];
            Variation variation = experiment.Variations[0];
            Decision decision = new Decision(variation.Id);

            UserProfile userProfile = new UserProfile(UserProfileId, new Dictionary<string, Decision>
            {
                { experiment.Id, decision }
            });

            UserProfileServiceMock.Setup(_ => _.Lookup(UserProfileId)).Returns(userProfile.ToMap());

            Bucketer bucketer = new Bucketer(LoggerMock.Object);
            DecisionService decisionService = new DecisionService(bucketer, ErrorHandlerMock.Object, NoAudienceProjectConfig, UserProfileServiceMock.Object, LoggerMock.Object);

            Assert.IsTrue(TestData.CompareObjects(variation, decisionService.GetVariation(experiment, UserProfileId, new UserAttributes())));

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, string.Format("Returning previously activated variation \"{0}\" of experiment \"{1}\" for user \"{2}\" from user profile.",
                variation.Key, experiment.Key, UserProfileId)));




            //    UserProfileService userProfileService = mock(UserProfileService.class);
            //when(userProfileService.lookup(userProfileId)).thenReturn(userProfile.toMap());

            //Bucketer bucketer = new Bucketer(noAudienceProjectConfig);
            //DecisionService decisionService = new DecisionService(bucketer,
            //        mockErrorHandler,
            //        noAudienceProjectConfig,
            //        userProfileService);

            //logbackVerifier.expectMessage(Level.INFO,
            //        "Returning previously activated variation \"" + variation.getKey() + "\" of experiment \"" + experiment.getKey() + "\""
            //                + " for user \"" + userProfileId + "\" from user profile.");

            //// ensure user with an entry in the user profile is bucketed into the corresponding stored variation
            //assertEquals(variation,
            //        decisionService.getVariation(experiment, userProfileId, Collections.<String, String>emptyMap()));

            //verify(userProfileService).lookup(userProfileId);
        }
            
        [Test]
        public void GetStoredVariationLogsWhenLookupReturnsNull()
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
        public void GetStoredVariationReturnsNullWhenVariationIsNoLongerInConfig()
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
        public void GetVariationSavesBucketedVariationIntoUserProfile() 
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
            mockBucketer.Setup(m => m.Bucket(ValidProjectConfig, experiment, UserProfileId)).Returns(variation);

            DecisionService decisionService = new DecisionService(mockBucketer.Object, ErrorHandlerMock.Object, ValidProjectConfig, UserProfileServiceMock.Object, LoggerMock.Object);

            Assert.IsTrue(TestData.CompareObjects(variation, decisionService.GetVariation(experiment, UserProfileId, new UserAttributes())));

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, string.Format("Saved variation \"{0}\" of experiment \"{1}\" for user \"{2}\".", variation.Id,
                        experiment.Id, UserProfileId)), Times.Once);
    }
        [Test]
        [ExpectedException]
        public void BucketLogsCorrectlyWhenUserProfileFailsToSave()
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
                ("Failed to save variation \"{0}\" of experiment \"{1}\" for user \"{2}\".", UserProfileId, variation.Id, experiment.Id))
                , Times.Once);
    }
        [Test]
        public void GetVariationSavesANewUserProfile()
        {
            Experiment experiment = NoAudienceProjectConfig.Experiments[0];
            Variation variation = experiment.Variations[0];
            Decision decision = new Decision(variation.Id);

            UserProfile expectedUserProfile = new UserProfile(UserProfileId, new Dictionary<string, Decision>
            {
                { experiment.Id, decision }
            });

            var mockBucketer = new Mock<Bucketer>(LoggerMock.Object);
            mockBucketer.Setup(m => m.Bucket(NoAudienceProjectConfig, experiment, UserProfileId)).Returns(variation);

            Dictionary<string, object> userProfile = null;
            
            UserProfileServiceMock.Setup(up => up.Lookup(UserProfileId)).Returns(userProfile);

            DecisionService decisionService = new DecisionService(mockBucketer.Object, ErrorHandlerMock.Object, NoAudienceProjectConfig,
                UserProfileServiceMock.Object, LoggerMock.Object);

            Assert.IsTrue(TestData.CompareObjects(variation, decisionService.GetVariation(experiment, UserProfileId, new UserAttributes())));
    }
    }
}