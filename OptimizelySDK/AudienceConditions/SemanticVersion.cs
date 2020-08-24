﻿/* 
 * Copyright 2020, Optimizely
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
using System;
using System.Collections.Generic;

namespace OptimizelySDK.AudienceConditions
{
    /// <summary>
    /// Extension class with methods to parse semantic version
    /// </summary>
    public static class SemanticVersionExtension
    {
        public const char BuildSeparator = '+';
        public const char PreReleaseSeparator = '-';

        /// <summary>
        /// Helper method to check if semantic version contains white spaces.
        /// </summary>
        /// <param name="semanticVersion">Semantic version</param>
        /// <returns>True if Semantic version contains white space, else false</returns>
        public static bool ContainsWhiteSpace(this string semanticVersion)
        {
            return semanticVersion.Contains(" ");
        }

        /// <summary>
        /// Helper method to check if semantic version contains prerelease version.
        /// </summary>
        /// <param name="semanticVersion">Semantic version</param>
        /// <returns>True if Semantic version contains '-', else false</returns>
        public static bool IsPreRelease(this string semanticVersion)
        {
            return semanticVersion.Contains(PreReleaseSeparator.ToString());
        }

        /// <summary>
        /// Helper method to check if semantic version contains build version.
        /// </summary>
        /// <param name="semanticVersion">Semantic version</param>
        /// <returns>True if Semantic version contains '+', else false</returns>
        public static bool IsBuild(this string semanticVersion)
        {
            return semanticVersion.Contains(BuildSeparator.ToString());
        }

        /// <summary>
        /// Helper method to parse and split semantic version into string array.
        /// </summary>
        /// <param name="semanticVersion">Semantic version</param>
        /// <returns>string array conatining major.minor.patch and prerelease or beta version as last element.</returns>
        public static string[] SplitSemanticVersion(this string version)
        {
            List<string> versionParts = new List<string>();
            // pre-release or build.
            string versionSuffix = string.Empty;
            string[] preVersionParts;
            if (version.ContainsWhiteSpace())
            {
                // log and throw error
                throw new Exception("Semantic version contains white spaces. Invalid Semantic Version.");
            }

            if (version.IsBuild() || version.IsPreRelease())
            {
                var partialVersionParts = version.Split(new char [] { version.IsPreRelease() ?
                     PreReleaseSeparator : BuildSeparator}, StringSplitOptions.RemoveEmptyEntries);
                if (partialVersionParts.Length <= 1)
                {
                    // throw error
                    throw new Exception("Invalid Semantic Version.");
                }
                // major.minor.patch
                var versionPrefix = partialVersionParts[0];
                versionSuffix = partialVersionParts[1];

                preVersionParts = versionPrefix.Split('.');
            }
            else
            {
                preVersionParts = version.Split('.');
            }

            if (preVersionParts.Length > 3)
            {
                // Throw error as pre version should only contain major.minor.patch version 
                throw new Exception("Invalid Semantic Version.");
            }

            versionParts.AddRange(preVersionParts);
            if (!string.IsNullOrEmpty(versionSuffix))
            {
                versionParts.Add(versionSuffix);
            }

            return versionParts.ToArray();
        }
    }

    /// <summary>
    /// IComparable class to compare user semantic versions with target semantic version.
    /// </summary>
    public class SemanticVersion : IComparable<SemanticVersion>
    {
        public string Version { get; private set; }

        public SemanticVersion(string version)
        {
            Version = version;
        }

        /// <summary>
        /// Compares user semantic version with targetedVersion.
        /// </summary>
        /// <param name="targetedVersion">String Semantic version</param>
        /// <returns> Integer value with value:
        /// -1 when targetedVersion is greater than UserVersion
        /// 0 when targetedVersion is equals to UserVersion
        /// +1 when targetedVersion is less than UserVersion
        /// </returns>
        public int CompareTo(SemanticVersion targetedVersion)
        {
            // Valid semantic version should not be null or empty
            if (targetedVersion == null || string.IsNullOrEmpty(targetedVersion.Version))
            {
                throw new Exception("Invalid target semantic version.");
            }

            if (string.IsNullOrEmpty(Version))
            {
                throw new Exception("Invalid user semantic version.");
            }

            var targetedVersionParts = targetedVersion.Version.SplitSemanticVersion();
            var userVersionParts = Version.SplitSemanticVersion();

            for (var index = 0; index < targetedVersionParts.Length; index++)
            {

                if (userVersionParts.Length <= index)
                {
                    return targetedVersion.Version.IsPreRelease() ? 1 : -1;
                }
                else
                {
                    if (!int.TryParse(userVersionParts[index], out int userVersionPartInt))
                    {
                        // Compare strings
                        int result = string.Compare(userVersionParts[index], targetedVersionParts[index]);
                        if (result != 0)
                        {
                            return result;
                        }
                    }
                    else if (int.TryParse(targetedVersionParts[index], out int targetVersionPartInt))
                    {
                        if (userVersionPartInt != targetVersionPartInt)
                        {
                            return userVersionPartInt < targetVersionPartInt ? -1 : 1;
                        }
                    }
                    else
                    {
                        return -1;
                    }
                }
            }

            if (!targetedVersion.Version.IsPreRelease() && Version.IsPreRelease())
            {
                return -1;
            }

            return 0;
        }
    }
}
