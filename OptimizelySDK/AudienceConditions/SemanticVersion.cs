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
    public class SemanticVersion : IComparable<SemanticVersion>
    {
        public readonly int? Major;

        public readonly int? Minor;

        public readonly int? Patch;

        /// <summary>
        /// Pre-release tags (potentially empty, but never null). This is private to
        /// ensure read only access.
        /// </summary>
        private readonly string[] PreRelease;

        /// <summary>
        /// Build meta data tags (potentially empty, but never null). This is private
        /// to ensure read only access.
        /// </summary>
        private readonly string[] BuildMeta;

        /// <summary>
        /// Construct a version object by parsing a string.
        /// </summary>
        /// <param name="version">version in flat string format.</param>
        public SemanticVersion(string version)
        {
            // Throw exception if version contains empty space
            if (version.Contains(" "))
            {
                throw new ParseException("Invalid Semantic Version.");
            }

            VParts = new int?[3];
            PreParts = new List<string>(5);
            MetaParts = new List<string>(5);
            Input = version.ToCharArray();
            if (!StateMajor())
            { // Start recursive descend
                throw new ParseException("Invalid Semantic Version.");
            }
            Major = VParts[0];
            Minor = VParts[1];
            Patch = VParts[2];
            PreRelease = PreParts.ToArray();
            BuildMeta = MetaParts.ToArray();
        }
        public int CompareTo(SemanticVersion targetVersion)
        {
            var result = Major - targetVersion.Major;
            if (result == 0)
            { // Same Major
                if (targetVersion.Minor != null)
                {
                    if (Minor != null)
                    {
                        result = Minor - targetVersion.Minor;
                        if (result == 0)
                        { // Same minor
                            if (targetVersion.Patch != null)
                            {
                                if (Patch != null)
                                {
                                    result = Patch - targetVersion.Patch;
                                    if (result == 0)
                                    { // Same patch
                                        if (PreRelease.Length == 0 && targetVersion.PreRelease.Length > 0)
                                        {
                                            result = 1; // No pre release wins over pre release
                                        }
                                        if (targetVersion.PreRelease.Length == 0 && PreRelease.Length > 0)
                                        {
                                            result = -1; // No pre release wins over pre release
                                        }
                                        if (PreRelease.Length > 0 && targetVersion.PreRelease.Length > 0)
                                        {
                                            int len = Math.Min(PreRelease.Length, targetVersion.PreRelease.Length);
                                            int count;
                                            for (count = 0; count < len; count++)
                                            {
                                                result = ComparePreReleaseTag(count, targetVersion);
                                                if (result != 0)
                                                {
                                                    break;
                                                }
                                            }
                                            if (result == 0 && count == len)
                                            { // Longer version wins.
                                                result = PreRelease.Length - targetVersion.PreRelease.Length;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    result = -1;
                                }
                            }
                        }
                    }
                    else
                    {
                        result = -1;
                    }
                }
            }
            return result ?? -1;
        }

        private int ComparePreReleaseTag(int pos, SemanticVersion ov)
        {
            var isHereParsed = int.TryParse(PreRelease[pos], out int here);
            var isThereParsed = int.TryParse(ov.PreRelease[pos], out int there);

            if (isHereParsed && !isThereParsed)
            {
                return -1; // Strings take precedence over numbers
            }
            if (!isHereParsed && isThereParsed)
            {
                return 1; // Strings take precedence over numbers
            }
            if (!isHereParsed)
            {
                return (PreRelease[pos].CompareTo(ov.PreRelease[pos])); // ASCII compare
            }

            return here.CompareTo(there); // Number compare
        }

        override public string ToString()
        {
            var ret = new StringBuilder();
            ret.Append(Major);
            if (Minor != null)
            {
                ret.Append('.');
                ret.Append(Minor);
            }
            if (Patch != null)
            {
                ret.Append('.');
                ret.Append(Patch);
            }
            if (PreRelease.Length > 0)
            {
                ret.Append('-');
                for (var i = 0; i < PreRelease.Length; i++)
                {
                    ret.Append(PreRelease[i]);
                    if (i < PreRelease.Length - 1)
                    {
                        ret.Append('.');
                    }
                }
            }
            if (BuildMeta.Length > 0)
            {
                ret.Append('+');
                for (int i = 0; i < BuildMeta.Length; i++)
                {
                    ret.Append(BuildMeta[i]);
                    if (i < BuildMeta.Length - 1)
                    {
                        ret.Append('.');
                    }
                }
            }
            return ret.ToString();
        }

        // Parser implementation below

        private readonly int?[] VParts;
        private readonly List<string> PreParts;
        private readonly List<string> MetaParts;
        private readonly char[] Input;

        private bool StateMajor()
        {
            var pos = 0;
            while (pos < Input.Length && Input[pos] >= '0' && Input[pos] <= '9')
            {
                pos++; // match [0..9]+
            }
            if (pos == 0)
            { // Empty String -> Error
                return false;
            }
            if (Input[0] == '0' && pos > 1)
            { // Leading zero
                return false;
            }
            if (int.TryParse(new string(Input, 0, pos), out int vPart))
            {
                VParts[0] = vPart;
            }
            if (Input.Length > pos && Input[pos] == '.')
            {
                return StateMinor(pos + 1);
            }
            else
            {
                VParts[1] = null;
                VParts[2] = null;
                return true;
            }
        }

        private bool StateMinor(int index)
        {
            int pos = index;
            while (pos < Input.Length && Input[pos] >= '0' && Input[pos] <= '9')
            {
                pos++;// match [0..9]+
            }
            if (pos == index)
            { // Empty String -> Error
                return false;
            }
            if (Input[0] == '0' && pos - index > 1)
            { // Leading zero
                return false;
            }
            if (int.TryParse(new string(Input, index, pos - index), out int vPart))
            {
                VParts[1] = vPart;
            }

            if (Input.Length > pos && Input[pos] == '.')
            {
                return StatePatch(pos + 1);
            }
            else
            {
                VParts[2] = null;
                return true;
            }
        }

        private bool StatePatch(int index)
        {
            int pos = index;
            while (pos < Input.Length && Input[pos] >= '0' && Input[pos] <= '9')
            {
                pos++; // match [0..9]+
            }
            if (pos == index)
            { // Empty String -> Error
                return false;
            }
            if (Input[0] == '0' && pos - index > 1)
            { // Leading zero
                return false;
            }

            if (int.TryParse(new string(Input, index, pos - index), out int vPart))
            {
                VParts[2] = vPart;
            }

            if (pos >= Input.Length)
            { // We have a clean version string
                return true;
            }

            if (Input[pos] == '+')
            { // We have build meta tags -> descend
                return StateMeta(pos + 1);
            }

            if (Input[pos] == '-')
            { // We have pre release tags -> descend
                return StateRelease(pos + 1);
            }

            // We have junk
            return false;
        }

        private bool StateRelease(int index)
        {
            int pos = index;
            while ((pos < Input.Length)
                && ((Input[pos] >= '0' && Input[pos] <= '9')
                || (Input[pos] >= 'a' && Input[pos] <= 'z')
                || (Input[pos] >= 'A' && Input[pos] <= 'Z') || Input[pos] == '-'))
            {
                pos++; // match [0..9a-zA-Z-]+
            }
            if (pos == index)
            { // Empty String -> Error
                return false;
            }

            PreParts.Add(new string(Input, index, pos - index));
            if (pos == Input.Length)
            { // End of input
                return true;
            }
            if (Input[pos] == '.')
            { // More parts -> descend
                return StateRelease(pos + 1);
            }
            if (Input[pos] == '+')
            { // Build meta -> descend
                return StateMeta(pos + 1);
            }

            return false;
        }

        private bool StateMeta(int index)
        {
            int pos = index;
            while ((pos < Input.Length)
                && ((Input[pos] >= '0' && Input[pos] <= '9')
                || (Input[pos] >= 'a' && Input[pos] <= 'z')
                || (Input[pos] >= 'A' && Input[pos] <= 'Z') || Input[pos] == '-'))
            {
                pos++; // match [0..9a-zA-Z-]+
            }
            if (pos == index)
            { // Empty String -> Error
                return false;
            }

            MetaParts.Add(new string(Input, index, pos - index));
            if (pos == Input.Length)
            { // End of input
                return true;
            }
            if (Input[pos] == '.')
            { // More parts -> descend
                return StateMeta(pos + 1);
            }
            return false;
        }
    }
}
