using System;
using System.Linq;
using VirtoCommerce.CoreModule.Core.Common;

namespace VirtoCommerce.MarketingModule.Core.Model.Promotions.Conditions
{
    //User groups contains condition
    public class UserGroupsContainsCondition : BaseCondition
    {
        public string Group { get; set; }

        /// <summary>
        ///  ((EvaluationContextBase)x).UserGroupsContains
        /// </summary>
        public override bool Evaluate(IEvaluationContext context)
        {
            var result = false;
            if (context is EvaluationContextBase evaluationContextBase)
            {
                result = evaluationContextBase.UserGroups != null;
                if (result)
                {
                    result = evaluationContextBase.UserGroups.Any(x => string.Equals(x, Group, StringComparison.InvariantCultureIgnoreCase));
                }
            }

            return result;
        }
    }
}
