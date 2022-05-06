using OptimizelySDK.Config.audience.match;
using OptimizelySDK.Entity;
using System;

namespace OptimizelySDK.Config.audience
{
    public class UserAttribute<T> : Condition
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Match { get; set; }
        public object Value { get; set; }

        public bool? Evaluate(ProjectConfig config, UserAttributes attributes)
        {
            if (attributes == null)
            {
                attributes = new UserAttributes();
            }
            // Valid for primitive types, but needs to change when a value is an object or an array
            object userAttributeValue = null;
            attributes.TryGetValue(Name, out userAttributeValue);

            if (!"custom_attribute".Equals(Type))
            {
                //logger.warn("Audience condition \"{}\" uses an unknown condition type. You may need to upgrade to a newer release of the Optimizely SDK.", this);
                return null; // unknown type
            }
            // check user attribute value is equal
            try
            {
                Match matcher = MatchRegistry.GetMatch(Match);
                bool? result = matcher.Eval(Value, userAttributeValue);
                if (result == null)
                {
                    throw new Exception("unknown value type");
                }

                return result;
            }
            catch (Exception e)
            {
                if (!attributes.ContainsKey(Name))
                {
                    //Missing attribute value
                   // logger.debug("Audience condition \"{}\" evaluated to UNKNOWN because no value was passed for user attribute \"{}\"", this, name);
                }
                else
                {
                    //if attribute value is not valid
                    if (userAttributeValue != null)
                    {
                        //logger.warn(
                          //  "Audience condition \"{}\" evaluated to UNKNOWN because a value of type \"{}\" was passed for user attribute \"{}\"",
                            //this,
                            //userAttributeValue.getClass().getCanonicalName(),
                            //name);
                    }
                    else
                    {
                        //logger.debug(
                          //  "Audience condition \"{}\" evaluated to UNKNOWN because a null value was passed for user attribute \"{}\"",
                            //this,
                            //name);
                    }
                }
            } 
            return null;
        }

        public string GetOperandOrId()
        {
            return null;
        }

        public string ToJson()
        {
            throw new NotImplementedException();
        }
    }
}
