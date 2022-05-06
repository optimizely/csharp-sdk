using System;
using System.Collections.Generic;

namespace OptimizelySDK.Config.audience.match
{
    public static class MatchRegistry
    {
        

        private static readonly string EXACT = "exact";
        private static readonly string EXISTS = "exists";
        private static readonly string GREATER_THAN = "gt";
        private static readonly string GREATER_THAN_EQ = "ge";
        private static readonly string LEGACY = "legacy";
        private static readonly string LESS_THAN = "lt";
        private static readonly string LESS_THAN_EQ = "le";
        private static readonly string SEMVER_EQ = "semver_eq";
        private static readonly string SEMVER_GE = "semver_ge";
        private static readonly string SEMVER_GT = "semver_gt";
        private static readonly string SEMVER_LE = "semver_le";
        private static readonly string SEMVER_LT = "semver_lt";
        private readonly static string SUBSTRING = "substring";

        
        private readonly static Dictionary<string, Match> Registry = new Dictionary<string, Match>
        {
            { EXACT, new ExactMatch() },
            { EXISTS, new ExisitsMatch() },
            { GREATER_THAN, new GTMatch() },
            { GREATER_THAN_EQ, new GEMatch() },
            { LEGACY, new DefaultMatchForLegacyAttributes() },
            { LESS_THAN, new LTMatch() },
            { LESS_THAN_EQ, new LEMatch() },
            { SUBSTRING, new SubstringMatch() }
        };

        // TODO rename Match to Matcher
        public static Match GetMatch(string name)
        {
            name = name == null ? LEGACY : name;
            Match match = null;
            Registry.TryGetValue(name, out match);
            if (match == null) {
                throw new Exception();
            }

            return match;
        }
        public static void Register(string name, Match match)
        {
            Registry[name] = match;
        }

    }
}
