using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using VirtoCommerce.NotificationsModule.Core.Events;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.NotificationsModule.Data.Validation;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Data.Infrastructure;

namespace VirtoCommerce.NotificationsModule.Data.Services
{

    public class NotificationService : ServiceBase, INotificationService, INotificationRegistrar
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly Func<INotificationRepository> _repositoryFactory;

        public NotificationService(Func<INotificationRepository> repositoryFactory, IEventPublisher eventPublisher)
        {
            _repositoryFactory = repositoryFactory;
            _eventPublisher = eventPublisher;
        }

        public async Task<Notification> GetByTypeAsync(string type, string tenantId = null, string tenantType = null, string responseGroup = null)
        {
            var notificationType = AbstractTypeFactory<Notification>.AllTypeInfos.FirstOrDefault(t => t.Type.Name.Equals(type))?.Type;
            if (notificationType == null)
            {
                return null;
            }

            var notificaionResponseGroup = EnumUtility.SafeParse(responseGroup, NotificationResponseGroup.Full);

            var result = AbstractTypeFactory<Notification>.TryCreateInstance(notificationType.Name);
            using (var repository = _repositoryFactory())
            {
                var notification = await repository.GetByTypeAsync(notificationType.Name, tenantId, tenantType, notificaionResponseGroup);
                if (notification != null)
                {
                    return notification.ToModel(result);
                }
            }
            return result;
        }

        public async Task<Notification[]> GetByIdsAsync(string[] ids, string responseGroup = null)
        {
            var notificaionResponseGroup = EnumUtility.SafeParse(responseGroup, NotificationResponseGroup.Full);

            using (var repository = _repositoryFactory())
            {
                var notifications = await repository.GetByIdsAsync(ids, notificaionResponseGroup);
                return notifications.Select(n => n.ToModel(AbstractTypeFactory<Notification>.TryCreateInstance(n.Type))).ToArray();
            }
        }

        public async Task SaveChangesAsync(Notification[] notifications)
        {
            if (notifications != null && notifications.Any())
            {
                ValidateNotificationProperties(notifications);

                var pkMap = new PrimaryKeyResolvingMap();
                var changedEntries = new List<ChangedEntry<Notification>>();
                using (var repository = _repositoryFactory())
                using (var changeTracker = new ObservableChangeTracker())
                {
                    var existingNotificationEntities = await repository.GetByIdsAsync(notifications.Select(m => m.Id).ToArray(), NotificationResponseGroup.Full);
                    foreach (var notification in notifications)
                    {
                        var dataTargetNotification = existingNotificationEntities.FirstOrDefault(n => n.Id.Equals(notification.Id));
                        var modifiedEntity = AbstractTypeFactory<NotificationEntity>.TryCreateInstance($"{notification.Kind}Entity").FromModel(notification, pkMap);

                        if (dataTargetNotification != null)
                        {
                            changeTracker.Attach(dataTargetNotification);
                            modifiedEntity?.Patch(dataTargetNotification);
                            changedEntries.Add(new ChangedEntry<Notification>(notification, EntryState.Modified));
                        }
                        else
                        {
                            repository.Add(modifiedEntity);
                            changedEntries.Add(new ChangedEntry<Notification>(notification, EntryState.Added));
                        }
                    }

                    await _eventPublisher.Publish(new GenericNotificationChangingEvent<Notification>(changedEntries));
                    CommitChanges(repository);
                    pkMap.ResolvePrimaryKeys();
                    await _eventPublisher.Publish(new GenericNotificationChangedEvent<Notification>(changedEntries));
                }
            }
        }


        public void RegisterNotification<T>() where T : Notification
        {
            if (AbstractTypeFactory<Notification>.AllTypeInfos.All(t => t.Type != typeof(T)))
            {
                AbstractTypeFactory<Notification>.RegisterType<T>();
            }
        }

        private void ValidateNotificationProperties(IEnumerable<Notification> notifications)
        {
            if (notifications == null)
            {
                throw new ArgumentNullException(nameof(notifications));
            }

            var validator = new NotificationValidator();
            foreach (var notification in notifications)
            {
                validator.ValidateAndThrow(notification);
            }
        }
    }
}
