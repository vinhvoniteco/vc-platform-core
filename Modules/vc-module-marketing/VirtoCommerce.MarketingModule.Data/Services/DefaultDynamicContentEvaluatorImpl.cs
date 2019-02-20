using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.MarketingModule.Core.Model;
using VirtoCommerce.MarketingModule.Core.Services;
using VirtoCommerce.MarketingModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Serialization;

namespace VirtoCommerce.MarketingModule.Data.Services
{
    public class DefaultDynamicContentEvaluatorImpl : IMarketingDynamicContentEvaluator
    {
        private readonly Func<IMarketingRepository> _repositoryFactory;
        private readonly IDynamicContentService _dynamicContentService;
        private readonly IExpressionSerializer _expressionSerializer;
        private readonly ILogger _logger;

        public DefaultDynamicContentEvaluatorImpl(Func<IMarketingRepository> repositoryFactory, IDynamicContentService dynamicContentService, IExpressionSerializer expressionSerializer, ILogger<DefaultDynamicContentEvaluatorImpl> logger)
        {
            _repositoryFactory = repositoryFactory;
            _dynamicContentService = dynamicContentService;
            _expressionSerializer = expressionSerializer;
            _logger = logger;
        }

        #region IMarketingDynamicContentEvaluator Members

        public async Task<DynamicContentItem[]> EvaluateItemsAsync(IEvaluationContext context)
        {
            if (!(context is DynamicContentEvaluationContext dynamicContext))
            {
                throw new ArgumentException("The context must be a DynamicContentEvaluationContext.");
            }
            if (dynamicContext.ToDate == default(DateTime))
            {
                dynamicContext.ToDate = DateTime.UtcNow;
            }
            var retVal = new List<DynamicContentItem>();
            using (var repository = _repositoryFactory())
            {
                var publishings = await repository.PublishingGroups
                                                       .Include(x => x.ContentItems)
                                                       .Where(x => x.IsActive)
                                                       .Where(x => x.StoreId == dynamicContext.StoreId)
                                                       .Where(x => (x.StartDate == null || dynamicContext.ToDate >= x.StartDate) && (x.EndDate == null || x.EndDate >= dynamicContext.ToDate))
                                                       .Where(x => x.ContentPlaces.Any(y => y.ContentPlace.Name == dynamicContext.PlaceName))
                                                       .OrderBy(x => x.Priority)
                                                       .ToArrayAsync();

                //Get content items ids for publishings without ConditionExpression
                var contentItemIds = publishings.Where(x => x.ConditionExpression == null)
                                                .SelectMany(x => x.ContentItems)
                                                .Select(x => x.DynamicContentItemId)
                                                .ToList();
                foreach (var publishing in publishings.Where(x => x.ConditionExpression != null))
                {
                    try
                    {
                        //Next step need filter assignments contains dynamicexpression
                        var condition = _expressionSerializer.DeserializeExpression<Func<IEvaluationContext, bool>>(publishing.ConditionExpression);
                        if (condition(context))
                        {
                            contentItemIds.AddRange(publishing.ContentItems.Select(x => x.DynamicContentItemId));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message, ex);
                    }
                }

                var dunamicContentItems = await _dynamicContentService.GetContentItemsByIdsAsync(contentItemIds.ToArray());
                retVal.AddRange(dunamicContentItems);
            }

            return retVal.ToArray();
        }

        #endregion
    }
}
