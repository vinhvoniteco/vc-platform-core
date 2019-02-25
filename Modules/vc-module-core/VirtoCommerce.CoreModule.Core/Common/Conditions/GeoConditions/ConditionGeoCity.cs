namespace VirtoCommerce.CoreModule.Core.Common.Conditions
{
    //City is []
    public class ConditionGeoCity : MatchedConditionBase
    {
        public override bool Evaluate(IEvaluationContext context)
        {
            var result = false;
            if (context is EvaluationContextBase evaluationContext)
            {
                result = UseMatchedCondition(evaluationContext.GeoCity);
            }

            return result;
        }
    }
}
