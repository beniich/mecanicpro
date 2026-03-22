using System;
using System.Collections.Generic;

namespace MecaPro.Domain.Common.Events;

/// <summary>
/// Event published when a notification needs to be sent.
/// </summary>
public record SendNotificationEvent
{
    public string UserId { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string Body { get; init; } = null!;
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string[] Channels { get; init; } = [];
    public Dictionary<string, object> Data { get; init; } = [];
    public string? TemplateId { get; init; }
}
