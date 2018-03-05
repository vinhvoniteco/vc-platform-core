using System;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using VirtoCommerce.NotificationsModule.Core.Abstractions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Data.Senders
{
    public class NotificationSender : INotificationSender
    {
        private readonly int _maxRetryAttempts = 3;
        private readonly INotificationService _notificationService;
        private readonly INotificationTemplateRender _notificationTemplateRender;
        private readonly INotificationMessageService _notificationMessageService;
        private readonly INotificationMessageSender _notificationMessageSender;
        private readonly ILogger<NotificationSender> _logger;

        public NotificationSender(INotificationService notificationService, INotificationTemplateRender notificationTemplateRender
            , INotificationMessageService notificationMessageService
            , INotificationMessageSender notificationMessageSender
            , ILogger<NotificationSender> logger)
        {
            _notificationService = notificationService;
            _notificationTemplateRender = notificationTemplateRender;
            _notificationMessageService = notificationMessageService;
            _notificationMessageSender = notificationMessageSender;
            _logger = logger;
        }

        public async Task<NotificationSendResult> SendNotificationAsync(Notification notification, string language)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));

            NotificationSendResult result = new NotificationSendResult();

            var activeNotification = await _notificationService.GetNotificationByTypeAsync(notification.Type);

            var message = AbstractTypeFactory<NotificationMessage>.TryCreateInstance($"{activeNotification.Kind}Message");
            message.LanguageCode = language;
            message.MaxSendAttemptCount = _maxRetryAttempts + 1;
            activeNotification.ToMessage(message, _notificationTemplateRender);

            NotificationMessage[] messages = { message };
            await _notificationMessageService.SaveNotificationMessages(messages);

            var policy = Policy.Handle<SmtpException>().WaitAndRetryAsync(_maxRetryAttempts, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                , (exception, timeSpan, retryCount, context) => {
                    _logger.LogError(exception, $"Retry {retryCount} of {context.PolicyKey}, due to: {exception}.");
                    message.LastSendError = exception?.Message;
                });
            
            var policyResult = await policy.ExecuteAndCaptureAsync(() =>
            {
                message.LastSendAttemptDate = DateTime.Now;
                message.SendAttemptCount++;
                return _notificationMessageSender.SendNotificationAsync(message);
            });

            if (policyResult.Outcome == OutcomeType.Successful)
            {
                result.IsSuccess = true;
                message.SendDate = DateTime.Now;
            }
            else
            {
                result.ErrorMessage = policyResult.FinalException?.Message;
            }

            await _notificationMessageService.SaveNotificationMessages(messages);

            return result;
        }
    }
}
