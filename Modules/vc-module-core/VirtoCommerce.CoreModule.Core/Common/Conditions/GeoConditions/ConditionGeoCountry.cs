namespace VirtoCommerce.CoreModule.Core.Common.Conditions
{
    //Country is []
    public class ConditionGeoCountry : MatchedConditionBase
    {
        public override bool Evaluate(IEvaluationContext context)
        {
            var result = false;
            if (context is EvaluationContextBase evaluationContext)
            {
                result = UseMatchedCondition(evaluationContext.GeoCountry);
            }

            return result;
        }
    }
}