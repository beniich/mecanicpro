using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace MecaPro.Notifications.Application;

public enum NotificationChannel { Email, SMS, Push, InApp }

public record NotificationRequest 
{ 
    public string UserId { get; set; } = null!; 
    public string Title { get; set; } = null!; 
    public string Body { get; set; } = null!; 
    public string? TemplateId { get; set; } 
    public NotificationChannel[] Channels { get; set; } = []; 
    public Dictionary<string, object> Data { get; set; } = []; 
    public string? Type { get; set; } 
    public string? ActionUrl { get; set; } 
    public string? Email { get; set; } 
    public string? PhoneNumber { get; set; } 
}

public interface INotificationService { Task SendAsync(NotificationRequest request); }
public interface IEmailService { Task SendAsync(string to, string subject, string html, string? text); Task SendTemplateAsync(string to, string templateId, Dictionary<string, object> data); }
public interface ISmsService { Task SendAsync(string to, string body); }

public class NotificationService(IEmailService email, ISmsService sms) : INotificationService
{
    public async Task SendAsync(NotificationRequest req)
    {
        if (req.Channels.Contains(NotificationChannel.Email) && !string.IsNullOrEmpty(req.Email))
            await email.SendTemplateAsync(req.Email, req.TemplateId ?? "", req.Data);

        if (req.Channels.Contains(NotificationChannel.SMS) && !string.IsNullOrEmpty(req.PhoneNumber))
            await sms.SendAsync(req.PhoneNumber, req.Body);

        // In-App and Push would typically go to a persistent store or SignalR hub
        // For now, we focus on external providers
    }
}

public class SendGridEmailService(IConfiguration config) : IEmailService
{
    private readonly SendGridClient _client = new(config["SendGrid:ApiKey"]);
    
    public async Task SendAsync(string to, string subject, string html, string? text)
    {
        var msg = new SendGridMessage 
        { 
            From = new EmailAddress(config["SendGrid:FromEmail"]), 
            Subject = subject, 
            HtmlContent = html, 
            PlainTextContent = text 
        };
        msg.AddTo(new EmailAddress(to));
        await _client.SendEmailAsync(msg);
    }

    public async Task SendTemplateAsync(string to, string templateId, Dictionary<string, object> data)
    {
        var msg = new SendGridMessage(); 
        msg.SetFrom(new EmailAddress(config["SendGrid:FromEmail"])); 
        msg.AddTo(new EmailAddress(to)); 
        msg.SetTemplateId(templateId); 
        msg.SetTemplateData(data);
        await _client.SendEmailAsync(msg);
    }
}

public class TwilioSmsService(IConfiguration config) : ISmsService
{
    public async Task SendAsync(string to, string body)
    {
        TwilioClient.Init(config["Twilio:AccountSid"], config["Twilio:AuthToken"]);
        await MessageResource.CreateAsync(
            to: new PhoneNumber(to), 
            from: new PhoneNumber(config["Twilio:FromNumber"]), 
            body: body);
    }
}
