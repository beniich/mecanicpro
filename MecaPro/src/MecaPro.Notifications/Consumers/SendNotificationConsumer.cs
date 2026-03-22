using MassTransit;
using MecaPro.Domain.Common.Events;
using MecaPro.Notifications.Application;
using System.Threading.Tasks;

namespace MecaPro.Notifications.Consumers;

public class SendNotificationConsumer(INotificationService notifications) : IConsumer<SendNotificationEvent>
{
    public async Task Consume(ConsumeContext<SendNotificationEvent> context)
    {
        var msg = context.Message;
        
        // Convert Event to Request Internal Logic
        var req = new NotificationRequest
        {
            UserId = msg.UserId,
            Title = msg.Title,
            Body = msg.Body,
            Email = msg.Email,
            PhoneNumber = msg.PhoneNumber,
            Channels = msg.Channels.Select(c => Enum.Parse<NotificationChannel>(c)).ToArray(),
            Data = msg.Data,
            TemplateId = msg.TemplateId
        };

        await notifications.SendAsync(req);
    }
}
