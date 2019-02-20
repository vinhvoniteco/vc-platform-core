using VirtoCommerce.CoreModule.Core.Common;

namespace VirtoCommerce.MarketingModule.Core.Model.Promotions
{
    //Get [] % off cart subtotal not to exceed $ [ 500 ]
    public class RewardCartGetOfRelSubtotal : BaseCondition, IRewardExpression
    {
        public decimal Amount { get; set; }
        public decimal MaxLimit { get; set; }

        #region IRewardsExpression Members

        public PromotionReward[] GetRewards()
        {
            var retVal = new CartSubtotalReward
            {
                Amount = Amount,
                AmountType = RewardAmountType.Relative,
                MaxLimit = MaxLimit
            };
            return new PromotionReward[] { retVal };
        }

        #endregion
    }
}
