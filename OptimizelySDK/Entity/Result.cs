using System;
using OptimizelySDK.OptimizelyDecisions;

namespace OptimizelySDK.Entity
{
    public class Result<T>
    {
        public T ResultObject;
        public DecisionReasons DecisionReasons;

        public static Result<T> NewResult(T resultObject, DecisionReasons decisionReasons)
        {
            return new Result<T> { DecisionReasons = decisionReasons, ResultObject = resultObject };
        }

        public Result<T> SetReasons(DecisionReasons decisionReasons)
        {
            DecisionReasons =  decisionReasons;

            return this;
        }
    }
}
