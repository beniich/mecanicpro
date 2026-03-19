// ============================================================
//  PHASE 10 — SIGNALR HUBS
//  ChatHub (mécanicien ↔ client via véhicule) + NotificationHub
// ============================================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MecaPro.Infrastructure.Persistence;

namespace MecaPro.API.Hubs;

// ─── CHAT HUB ────────────────────────────────────────────────
[Authorize]
public class ChatHub(AppDbContext db, ILogger<ChatHub> logger) : Hub
{
    // Join a vehicle conversation room
    public async Task JoinVehicleRoom(string vehicleId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"vehicle:{vehicleId}");

    public async Task LeaveVehicleRoom(string vehicleId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"vehicle:{vehicleId}");

    // Send message to vehicle room (visible to mechanic + customer)
    public async Task SendMessage(string vehicleId, string content, string? attachmentUrl = null)
    {
        var senderIdStr = Context.UserIdentifier!;
        var room     = $"vehicle:{vehicleId}";

        // Persist message
        var msg = new ChatMessage 
        { 
            Id = Guid.NewGuid(),
            SenderId = senderIdStr,
            RecipientId = "",
            Content = content + (attachmentUrl != null ? $" [Attachment: {attachmentUrl}]" : ""),
            VehicleId = Guid.Parse(vehicleId),
            SentAt = DateTime.UtcNow
        };
        await db.ChatMessages.AddAsync(msg);
        await db.SaveChangesAsync();

        // Broadcast to room (excluding sender if desired)
        await Clients.Group(room).SendAsync("OnMessage", new
        {
            id           = msg.Id,
            senderId     = senderIdStr,
            content      = msg.Content,
            attachmentUrl = attachmentUrl,
            sentAt       = msg.SentAt,
            vehicleId    = vehicleId
        });

        logger.LogInformation("Chat: {SenderId} → vehicle {VehicleId}", senderIdStr, vehicleId);
    }

    public async Task MarkRead(string vehicleId, string messageId)
    {
        if (!Guid.TryParse(messageId, out var msgId)) return;
        var msg = await db.ChatMessages.FindAsync(msgId);
        if (msg != null) { 
            msg.IsRead = true;
            msg.ReadAt = DateTime.UtcNow;
            await db.SaveChangesAsync(); 
        }
        await Clients.OthersInGroup($"vehicle:{vehicleId}").SendAsync("OnMessageRead", messageId);
    }

    // Get chat history for a vehicle
    public async Task<List<object>> GetHistory(string vehicleId, int skip = 0, int take = 50)
    {
        if (!Guid.TryParse(vehicleId, out var vid)) return [];
        return await db.ChatMessages
            .Where(m => m.VehicleId == vid)
            .OrderBy(m => m.SentAt)
            .Skip(skip).Take(take)
            .Select(m => (object)new
            {
                id = m.Id, senderId = m.SenderId, content = m.Content,
                isRead = m.IsRead, sentAt = m.SentAt, attachmentUrl = (string?)null
            })
            .ToListAsync();
    }

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("SignalR connected: {ConnectionId} User={User}",
            Context.ConnectionId, Context.UserIdentifier);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("SignalR disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}

// ─── NOTIFICATION HUB ────────────────────────────────────────
[Authorize]
public class NotificationHub(ILogger<NotificationHub> logger) : Hub
{
    // Subscribe to personal notifications
    public async Task Subscribe() =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{Context.UserIdentifier}");

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        logger.LogInformation("NotificationHub connected: {UserId}", userId);
        await base.OnConnectedAsync();
    }
}
