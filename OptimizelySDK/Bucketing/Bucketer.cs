/* 
 * Copyright 2017, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
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
using OptimizelySDK.Entity;
using OptimizelySDK.Logger;
using OptimizelySDK.OptimizelyDecisions;
using System;
using System.Collections.Generic;
using System.Text;

namespace OptimizelySDK.Bucketing
{
    public class Bucketer
    {
        /// <summary>
        /// Seed to be used in bucketing hash
        /// </summary>
        private const uint HASH_SEED = 1;

        /// <summary>
        /// Maximum traffic allocation value
        /// </summary>
        private const uint MAX_TRAFFIC_VALUE = 10000;

        /// <summary>
        /// Maximum possible hash value
        /// </summary>
        private static ulong MAX_HASH_VALUE = 0x100000000;

        private ILogger Logger;

        public Bucketer(ILogger logger)
        {
            Logger = logger;
        }


        /// <summary>
        /// Generate a hash value to be used in determining which variation the user will be put in
        /// </summary>
        /// <param name="bucketingKey">string value used for the key of the murmur hash.</param>
        /// <returns>integer Unsigned value denoting the hash value for the user</returns>
        private uint GenerateHashCode(string bucketingKey)
        {
            var murmer32 = Murmur.MurmurHash.Create32(seed: HASH_SEED, managed: true);
            byte[] data = Encoding.UTF8.GetBytes(bucketingKey);
            byte[] hash = murmer32.ComputeHash(data);
            return BitConverter.ToUInt32(hash, 0);
        }


        /// <summary>
        /// Generate an integer to be used in bucketing user to a particular variation
        /// </summary>
        /// <param name="bucketingKey">string Value used for the key of the murmur hash.</param>
        /// <returns>integer Value in the closed range [0, 9999] denoting the bucket the user belongs to</returns>
        public virtual int GenerateBucketValue(string bucketingKey)
        {
            uint hashCode = GenerateHashCode(bucketingKey);
            double ratio = hashCode / (double)MAX_HASH_VALUE;
            return (int)(ratio * MAX_TRAFFIC_VALUE);
        }

        /// <summary>
        /// Find the bucket for the user and group/experiment given traffic allocations
        /// </summary>
        /// <param name="bucketingId">A customer-assigned value used to create the key for the murmur hash.</param>
        /// <param name="userId">string ID for user</param>
        /// <param name="parentId">mixed ID representing Experiment or Group</param>
        /// <param name="trafficAllocations">array Traffic allocations for variation or experiment</param>
        /// <returns>string ID representing experiment or variation, returns null if not found</returns>
        private string FindBucket(string bucketingId, string userId, string parentId, IEnumerable<TrafficAllocation> trafficAllocations)
        {
            // Generate the bucketing key based on combination of user ID and experiment ID or group ID.
            string bucketingKey = bucketingId + parentId;
            int bucketingNumber = GenerateBucketValue(bucketingKey);

            Logger.Log(LogLevel.DEBUG, $"Assigned bucket [{bucketingNumber}] to user [{userId}] with bucketing ID [{bucketingId}].");

            foreach (var ta in trafficAllocations)
                if (bucketingNumber < ta.EndOfRange)
                    return ta.EntityId;

            return null;
        }

        /// <summary>
        /// Determine variation the user should be put in.
        /// </summary>
        /// <param name="config">ProjectConfig Configuration for the project</param>
        /// <param name="experiment">Experiment Experiment in which user is to be bucketed</param>
        /// <param name="bucketingId">A customer-assigned value used to create the key for the murmur hash.</param>
        /// <param name="userId">User identifier</param>
        /// <returns>Variation which will be shown to the user</returns>
        public virtual Result<Variation> Bucket(ProjectConfig config, Experiment experiment, string bucketingId, string userId)
        {
            string message;
            Variation variation;

            var reasons = new DecisionReasons();

            if (string.IsNullOrEmpty(experiment.Key))
                return Result<Variation>.NewResult(new Variation(), reasons);

            // Determine if experiment is in a mutually exclusive group.
            if (experiment.IsInMutexGroup)
            {
                Group group = config.GetGroup(experiment.GroupId);
                if (string.IsNullOrEmpty(group.Id))
                    return Result<Variation>.NewResult(new Variation(), reasons);

                string userExperimentId = FindBucket(bucketingId, userId, group.Id, group.TrafficAllocation);
                if (string.IsNullOrEmpty(userExperimentId))
                {
                    message = $"User [{userId}] is in no experiment.";
                    Logger.Log(LogLevel.INFO, reasons.AddInfo(message));
                    return Result<Variation>.NewResult(new Variation(), reasons);
                }

                if (userExperimentId != experiment.Id)
                {
                    message = $"User [{userId}] is not in experiment [{experiment.Key}] of group [{experiment.GroupId}].";
                    Logger.Log(LogLevel.INFO, reasons.AddInfo(message));
                    return Result<Variation>.NewResult(new Variation(), reasons);
                }

                message = $"User [{userId}] is in experiment [{experiment.Key}] of group [{experiment.GroupId}].";
                Logger.Log(LogLevel.INFO, reasons.AddInfo(message));
            }

            // Bucket user if not in whitelist and in group (if any).
            string variationId = FindBucket(bucketingId, userId, experiment.Id, experiment.TrafficAllocation);
            if (string.IsNullOrEmpty(variationId))
            {
                Logger.Log(LogLevel.INFO, reasons.AddInfo($"User [{userId}] is in no variation."));
                return Result<Variation>.NewResult(new Variation(), reasons);

            }

            // success!
            variation = config.GetVariationFromId(experiment.Key, variationId);
            message = $"User [{userId}] is in variation [{variation.Key}] of experiment [{experiment.Key}].";
            Logger.Log(LogLevel.INFO, reasons.AddInfo(message));
            return Result<Variation>.NewResult(variation, reasons);
        }
    }
}