using System;
using OptimizelySDK.OptimizelyDecisions;

namespace OptimizelySDK.Entity
{
    public class Result<T>
    {
        public T ResultObject;
        public IDecisionReasons DecisionReasons;

        public static Result<T> NewResult(T resultObject, IDecisionReasons decisionReasons)
        {
            return new Result<T> { DecisionReasons = decisionReasons, ResultObject = resultObject };
        }
    }
}
