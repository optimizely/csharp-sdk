/* 
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
using OptimizelySDK.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace OptimizelySDK.AudienceConditions
{
    public static class SemanticVersionExtension
    {
        public const char BuildSeparator = '+';
        public const char PreReleaseSeparator = '-';

        public static bool ContainsWhiteSpace(this string semanticVersion)
        {
            return semanticVersion.Contains(" ");
        }

        public static bool IsPreRelease(this string semanticVersion)
        {
            return semanticVersion.Contains(PreReleaseSeparator.ToString());
        }

        public static bool IsBuild(this string semanticVersion)
        {
            return semanticVersion.Contains(BuildSeparator.ToString());
        }

        public static int DotCount(this string semanticVersion)
        {
            return semanticVersion.Split('.').Length;
        }

        public static string[] SplitSemanticVersion(this string version)
        {
            List<string> versionParts = new List<string>();
            // major.minor.patch
            string versionPrefix = string.Empty;
            // pre-release or build.
            string versionSuffix = string.Empty;

            if (version.ContainsWhiteSpace()) {
                // log and throw error
                throw new Exception("");
            }

            if (version.IsBuild() || version.IsPreRelease()) {
                var partialVersionParts = version.Split(version.IsPreRelease() ?
                    SemanticVersionExtension.PreReleaseSeparator : SemanticVersionExtension.BuildSeparator);
                if (partialVersionParts.Length <= 1) {
                    // throw error
                    throw new Exception("");
                }
                versionPrefix = partialVersionParts[0];
                versionSuffix = partialVersionParts[1];
            }

            var preVersionParts = versionPrefix.Split('.');

            if (preVersionParts.Length > 2) {
                // throw error
                throw new Exception(" ");
            }

            versionParts.AddRange(preVersionParts);
            versionParts.Add(versionSuffix);


            return versionParts.ToArray();
        }
    }

    public class SemanticVersionV2 : IComparable<SemanticVersionV2>
    {
        public string Version { get; private set; }

        public int CompareTo(SemanticVersionV2 targetedVersion)
        {

            if (targetedVersion == null || string.IsNullOrEmpty(targetedVersion.Version)) {
                throw new Exception("empty");
            }

            if (string.IsNullOrEmpty(this.Version)) {
                throw new Exception(" Empty ");
            }

            var userVersionParts = targetedVersion.Version.SplitSemanticVersion();
            var targetedVersionParts = Version.SplitSemanticVersion();

            for (var index = 0; index < targetedVersionParts.Length; index++) {

                if (userVersionParts.Length <= index) {
                    return Version.IsPreRelease() ? 1 : -1;
                } else {
                    int userVersionPartInt;
                    int targetVersionPartInt;

                    if (!int.TryParse(userVersionParts[index], out userVersionPartInt)) {

                        int result = string.Compare(userVersionParts[index], targetedVersionParts[index]);
                        if (result != 0) {
                            return result;
                        }
                    } else {
                        int.TryParse(targetedVersionParts[index], out targetVersionPartInt);

                        if (userVersionPartInt != targetVersionPartInt) {
                            return userVersionPartInt < targetVersionPartInt ? -1 : 1;
                        }
                    }
                }
            }

            if (targetedVersion.Version.IsPreRelease() && !Version.IsPreRelease()) {
                return -1;
            }

            return 0;
        }
    }
}
