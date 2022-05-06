using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimizelySDK.Config.audience.match
{
    public class MatchRegistry
    {
        private readonly static Dictionary<string, Match> Registry = new Dictionary<string, Match>        
        {
            { EXACT, new ExactMatch() }
        };


        public readonly static string EXACT = "exact";
        public static readonly string EXISTS = "exists";
        public static readonly string GREATER_THAN = "gt";
        public static readonly string GREATER_THAN_EQ = "ge";
        public static readonly string LEGACY = "legacy";
        public static readonly string LESS_THAN = "lt";
        public static readonly string LESS_THAN_EQ = "le";
        public static readonly string SEMVER_EQ = "semver_eq";
        public static readonly string SEMVER_GE = "semver_ge";
        public static readonly string SEMVER_GT = "semver_gt";
        public static readonly string SEMVER_LE = "semver_le";
        public static readonly string SEMVER_LT = "semver_lt";
        public static readonly string SUBSTRING = "substring";

        // TODO rename Match to Matcher
        public static Match GetMatch(string name)
        {
            Match match = Registry[name == null ? LEGACY : name];
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
