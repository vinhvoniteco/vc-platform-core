namespace VirtoCommerce.CoreModule.Core.Common.Conditions
{
    //Browsing from a time zone -/+ offset from UTC 
    public class ConditionGeoTimeZone : CompareConditionBase
    {
        public int Value { get; set; }
        public int SecondValue { get; set; }

        public override bool Evaluate(IEvaluationContext context)
        {
            var result = false;
            if (context is EvaluationContextBase evaluationContext && int.TryParse(evaluationContext.GeoTimeZone, out var geoTimeZone))
            {
                result = UseCompareCondition(geoTimeZone, Value, SecondValue);
            }

            return result;
        }
    }
}
