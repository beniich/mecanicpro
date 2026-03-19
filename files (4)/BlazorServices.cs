// ============================================================
// BLAZOR SERVICES — ApiClient, AuthService, ChatService,
//                  NotificationService, ToastService
// ============================================================

// ─────────────────────────────────────────────────────────────
// API CLIENT — HTTP wrapper with JWT + refresh
// ─────────────────────────────────────────────────────────────

namespace MecaPro.Blazor.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly AuthStateProvider _authState;
    private readonly NavigationManager _nav;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(HttpClient http, AuthStateProvider authState,
        NavigationManager nav, ILogger<ApiClient> logger)
    { _http = http; _authState = authState; _nav = nav; _logger = logger; }

    public async Task<T?> GetAsync<T>(string url)
    {
        try
        {
            var response = await _http.GetAsync(url);
            return await HandleResponse<T>(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "GET {Url} failed", url);
            throw;
        }
    }

    public async Task<T?> PostAsync<T>(string url, object? body)
    {
        var json = body != null
            ? new StringContent(System.Text.Json.JsonSerializer.Serialize(body),
                System.Text.Encoding.UTF8, "application/json")
            : null;
        var response = await _http.PostAsync(url, json);
        return await HandleResponse<T>(response);
    }

    public async Task<T?> PutAsync<T>(string url, object? body)
    {
        var json = body != null
            ? new StringContent(System.Text.Json.JsonSerializer.Serialize(body),
                System.Text.Encoding.UTF8, "application/json")
            : null;
        var response = await _http.PutAsync(url, json);
        return await HandleResponse<T>(response);
    }

    public async Task<bool> DeleteAsync(string url)
    {
        var response = await _http.DeleteAsync(url);
        return response.IsSuccessStatusCode;
    }

    public async Task<byte[]?> GetBytesAsync(string url)
    {
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<string?> UploadImageAsync(string url, byte[] data,
        string fileName, string contentType)
    {
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent(data);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        var response = await _http.PostAsync(url, form);
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadAsStringAsync();
        return result;
    }

    private async Task<T?> HandleResponse<T>(HttpResponseMessage response)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Try refresh
            var refreshed = await _authState.TryRefreshAsync();
            if (!refreshed)
            {
                _nav.NavigateTo("/login");
                return default;
            }
            // Retry once
            var req = new HttpRequestMessage(response.RequestMessage!.Method,
                response.RequestMessage.RequestUri);
            var retry = await _http.SendAsync(req);
            response = retry;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.PaymentRequired)
        {
            _nav.NavigateTo("/subscription?upgrade=true");
            return default;
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("API error {Status}: {Body}", response.StatusCode, errorBody);
            return default;
        }

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content)) return default;
        return System.Text.Json.JsonSerializer.Deserialize<T>(content,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}

// ─────────────────────────────────────────────────────────────
// AUTH STATE PROVIDER
// ─────────────────────────────────────────────────────────────

public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _storage;
    private readonly HttpClient _http;

    public AuthStateProvider(ILocalStorageService storage, HttpClient http)
    { _storage = storage; _http = http; }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _storage.GetItemAsync<string>("access_token");
        if (string.IsNullOrWhiteSpace(token))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var claims = ParseJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task LoginAsync(AuthResponseDto auth)
    {
        await _storage.SetItemAsync("access_token", auth.AccessToken);
        await _storage.SetItemAsync("refresh_token", auth.RefreshToken);
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.AccessToken);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task LogoutAsync()
    {
        await _storage.RemoveItemAsync("access_token");
        await _storage.RemoveItemAsync("refresh_token");
        _http.DefaultRequestHeaders.Authorization = null;
        NotifyAuthenticationStateChanged(Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
    }

    public async Task<bool> TryRefreshAsync()
    {
        var refreshToken = await _storage.GetItemAsync<string>("refresh_token");
        if (string.IsNullOrEmpty(refreshToken)) return false;
        try
        {
            var response = await _http.PostAsJsonAsync("/api/v1/auth/refresh",
                new { RefreshToken = refreshToken });
            if (!response.IsSuccessStatusCode) return false;
            var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            if (auth == null) return false;
            await LoginAsync(auth);
            return true;
        }
        catch { return false; }
    }

    private static IEnumerable<Claim> ParseJwt(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3) return Enumerable.Empty<Claim>();
        var payload = parts[1];
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        return dict?.Select(kv => new Claim(kv.Key, kv.Value?.ToString() ?? ""))
               ?? Enumerable.Empty<Claim>();
    }
}

// ─────────────────────────────────────────────────────────────
// AUTH SERVICE (login/register calls)
// ─────────────────────────────────────────────────────────────

public class AuthService
{
    private readonly HttpClient _http;
    private readonly AuthStateProvider _authState;

    public AuthService(HttpClient http, AuthStateProvider authState)
    { _http = http; _authState = authState; }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("/api/v1/auth/login",
            new { email, password });
        if (!response.IsSuccessStatusCode)
            return AuthResult.Fail("Email ou mot de passe incorrect.");

        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        if (result == null) return AuthResult.Fail("Erreur serveur.");

        if (result.Requires2Fa)
            return AuthResult.Requires2Fa(result.TempToken!);

        await _authState.LoginAsync(result.AsAuth());
        return AuthResult.Ok();
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest req)
    {
        var response = await _http.PostAsJsonAsync("/api/v1/auth/register", req);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return AuthResult.Fail(string.Join(", ", err?.Errors ?? new[] { "Erreur" }));
        }
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        await _authState.LoginAsync(auth!);
        return AuthResult.Ok();
    }

    public async Task<AuthResult> Verify2FAAsync(string tempToken, string code)
    {
        var response = await _http.PostAsJsonAsync("/api/v1/auth/2fa/verify",
            new { tempToken, code });
        if (!response.IsSuccessStatusCode) return AuthResult.Fail("Code 2FA invalide.");
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        await _authState.LoginAsync(auth!);
        return AuthResult.Ok();
    }

    public async Task LogoutAsync()
    {
        var refreshToken = "";  // get from storage
        await _http.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken });
        await _authState.LogoutAsync();
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync()
    {
        try { return await _http.GetFromJsonAsync<CurrentUserDto>("/api/v1/auth/me"); }
        catch { return null; }
    }
}

// ─────────────────────────────────────────────────────────────
// CHAT SERVICE (SignalR)
// ─────────────────────────────────────────────────────────────

public class ChatService : IAsyncDisposable
{
    private HubConnection? _hub;
    private readonly ILocalStorageService _storage;
    private readonly NavigationManager _nav;

    public event Action<ChatMessageDto>? OnMessageReceived;
    public event Action<string>? OnTypingReceived;

    public ChatService(ILocalStorageService storage, NavigationManager nav)
    { _storage = storage; _nav = nav; }

    public async Task ConnectAsync()
    {
        var token = await _storage.GetItemAsync<string>("access_token");
        _hub = new HubConnectionBuilder()
            .WithUrl($"{_nav.BaseUri}hubs/chat?access_token={token}")
            .WithAutomaticReconnect()
            .Build();

        _hub.On<ChatMessageDto>("ReceiveMessage", msg =>
        {
            OnMessageReceived?.Invoke(msg);
        });

        _hub.On<string>("UserTyping", userId =>
        {
            OnTypingReceived?.Invoke(userId);
        });

        await _hub.StartAsync();
    }

    public async Task SendAsync(string recipientId, string content, string? vehicleId = null)
    {
        if (_hub?.State == HubConnectionState.Connected)
            await _hub.InvokeAsync("SendMessage", recipientId, content, vehicleId);
    }

    public async Task SendTypingAsync(string recipientId)
    {
        if (_hub?.State == HubConnectionState.Connected)
            await _hub.InvokeAsync("SendTyping", recipientId);
    }

    public async Task MarkAsReadAsync(string userId)
    {
        if (_hub?.State == HubConnectionState.Connected)
            await _hub.InvokeAsync("MarkConversationRead", userId);
    }

    public async Task DisconnectAsync()
    {
        if (_hub != null) await _hub.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub != null) await _hub.DisposeAsync();
    }
}

// ─────────────────────────────────────────────────────────────
// NOTIFICATION SERVICE (SignalR)
// ─────────────────────────────────────────────────────────────

public class NotificationService : IAsyncDisposable
{
    private HubConnection? _hub;
    private readonly ILocalStorageService _storage;
    private readonly NavigationManager _nav;
    private readonly ApiClient _api;

    public event Action<NotificationDto>? OnNotificationReceived;
    public event Action<ChatMessageDto>? OnMessageReceived;

    public NotificationService(ILocalStorageService storage, NavigationManager nav, ApiClient api)
    { _storage = storage; _nav = nav; _api = api; }

    public async Task ConnectAsync()
    {
        var token = await _storage.GetItemAsync<string>("access_token");
        _hub = new HubConnectionBuilder()
            .WithUrl($"{_nav.BaseUri}hubs/notifications?access_token={token}")
            .WithAutomaticReconnect()
            .Build();

        _hub.On<NotificationDto>("ReceiveNotification", n => OnNotificationReceived?.Invoke(n));
        _hub.On<ChatMessageDto>("NewMessage", m => OnMessageReceived?.Invoke(m));

        await _hub.StartAsync();
    }

    public async Task<int> GetUnreadMessageCountAsync()
    {
        var convs = await _api.GetAsync<List<ConversationDto>>("/api/v1/chat/conversations");
        return convs?.Sum(c => c.UnreadCount) ?? 0;
    }

    public async Task<int> GetUnreadNotifCountAsync()
    {
        var notifs = await _api.GetAsync<List<NotificationDto>>("/api/v1/notifications?unreadOnly=true");
        return notifs?.Count ?? 0;
    }

    public async Task<int> GetActivePannesCountAsync()
    {
        var stats = await _api.GetAsync<DashboardStatsDto>("/api/v1/dashboard/stats");
        return stats?.ActiveDiagnostics ?? 0;
    }

    public async Task MarkAllReadAsync()
    {
        if (_hub?.State == HubConnectionState.Connected)
            await _hub.InvokeAsync("MarkAllRead");
    }

    public async Task DisconnectAsync()
    {
        if (_hub != null) await _hub.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub != null) await _hub.DisposeAsync();
    }
}

// ─────────────────────────────────────────────────────────────
// TOAST SERVICE
// ─────────────────────────────────────────────────────────────

public enum ToastType { Success, Error, Warning, Info }

public class ToastService
{
    public event Action<string, ToastType>? OnShow;
    public void Show(string message, ToastType type = ToastType.Info)
        => OnShow?.Invoke(message, type);
    public void Success(string msg) => Show(msg, ToastType.Success);
    public void Error(string msg) => Show(msg, ToastType.Error);
    public void Warning(string msg) => Show(msg, ToastType.Warning);
}

// ─────────────────────────────────────────────────────────────
// ALL DTOs
// ─────────────────────────────────────────────────────────────

public record DashboardStatsDto(
    int VehiclesInProgress, int ActiveDiagnostics, int CompletedRevisions,
    decimal MonthlyRevenue, int TotalClients, int LowStockParts,
    int TodayRevisions, int UnreadMessages);

public record VehicleSummaryDto(
    Guid Id, string LicensePlate, string? VIN, string Make, string Model,
    int Year, int Mileage, string? FuelType, string? Color,
    string CurrentStatus, string QrCodeToken,
    string CustomerName, Guid CustomerId,
    DateTime? NextRevisionDate, int ActiveDiagnosticsCount,
    bool HasCriticalDiagnostic);

public record VehicleDetailDto(
    Guid Id, string LicensePlate, string? VIN, string Make, string Model,
    int Year, int Mileage, string? FuelType, string? Color, string CurrentStatus,
    Guid CustomerId, string CustomerName, string CustomerEmail, string? CustomerPhone,
    string CustomerInitials, int CustomerVehicleCount, int CustomerLoyaltyPoints,
    decimal CustomerTotalSpent,
    IEnumerable<DiagnosticDto> ActiveDiagnostics,
    IEnumerable<RevisionDto> RevisionHistory,
    IEnumerable<VehicleImageDto> Images);

public record DiagnosticDto(
    Guid Id, string FaultCode, string Description, string Severity,
    string Status, string? DiagnosticTool, string? ProbableCauses,
    string? Resolution, DateTime DiagnosedAt, DateTime? ResolvedAt);

public record RevisionDto(
    Guid Id, string Type, DateTime ScheduledDate, DateTime? CompletedDate,
    int EstimatedMinutes, int? ActualMinutes, decimal EstimatedCost,
    decimal? ActualCost, string Status, string? Notes,
    string? VehiclePlate, string? CustomerName, string StatusClass);

public record RevisionSummaryDto(
    Guid Id, string Type, DateTime ScheduledDate, int EstimatedMinutes,
    decimal EstimatedCost, string Status, string StatusClass,
    string VehiclePlate, string CustomerName);

public record VehicleImageDto(Guid Id, string FileName, string BlobUrl, string? Description, DateTime UploadedAt);

public record CustomerDto(
    Guid Id, string FirstName, string LastName, string Email, string? Phone,
    string Segment, int LoyaltyPoints, int VehicleCount, decimal TotalSpent, DateTime CreatedAt);

public record CrmStatsDto(int Total, int GoldPlus, decimal AvgLifetimeValue, int NewThisMonth);

public record ConversationDto
{
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Initials { get; set; } = "";
    public string? VehiclePlate { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? CustomerId { get; set; }
    public string LastMessage { get; set; } = "";
    public DateTime LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}

public record ChatMessageDto
{
    public Guid Id { get; set; }
    public string SenderId { get; set; } = "";
    public string Content { get; set; } = "";
    public bool IsOwn { get; set; }
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }
    public Guid? VehicleId { get; set; }
    public string? VehiclePlate { get; set; }
}

public record NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string Type { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ActionUrl { get; set; }
}

public record SubscriptionDto(
    Guid Id, string PlanName, string PlanTier, string Status,
    decimal PriceMonthly, DateTime StartDate, DateTime? NextRenewal,
    int MaxMechanics, int MechanicsUsed);

public record PlanDto(
    Guid Id, string Name, string Tier, decimal PriceMonthly, decimal PriceYearly,
    int MaxMechanics, int MaxVehicles, bool HasEcommerce, bool HasApiAccess,
    bool IsWhiteLabel, bool IsFeatured, bool IsCurrentPlan);

public record InvoiceDto(Guid Id, string Number, decimal TotalTTC, string Status, DateTime IssuedAt);

public record CheckoutSessionDto(string SessionId, string Url);
public record PortalSessionDto(string Url);

public record AlertDto(string Title, string Description, string Type, string ActionUrl);
public record RevenueDataPoint(string Month, decimal Revisions, decimal Parts);

public record AuthResult
{
    public bool Success { get; init; }
    public bool Need2Fa { get; init; }
    public string? TempToken { get; init; }
    public string? ErrorMessage { get; init; }

    public static AuthResult Ok() => new() { Success = true };
    public static AuthResult Fail(string msg) => new() { ErrorMessage = msg };
    public static AuthResult Requires2Fa(string token) => new() { Need2Fa = true, TempToken = token };
}

public record CurrentUserDto(string Id, string Email, string FirstName, string LastName,
    List<string> Roles, string SubscriptionTier, Guid GarageId);

public record RegisterRequest(string FirstName, string LastName, string Email,
    string Password, string ConfirmPassword, Guid? GarageId = null);

public record ErrorResponse(string[]? Errors);

public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize, int TotalPages);

// ─────────────────────────────────────────────────────────────
// PROGRAM.CS BLAZOR WASM
// ─────────────────────────────────────────────────────────────

/*
// Program.cs (Blazor WASM)
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HTTP Client with base address
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.Configuration["API:BaseUrl"] ?? builder.HostEnvironment.BaseAddress)
});

// Auth
builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<AuthStateProvider>());
builder.Services.AddAuthorizationCore(opt =>
{
    opt.AddPolicy("RequireMechanic",
        p => p.RequireRole("Mechanic", "GarageOwner", "SuperAdmin"));
    opt.AddPolicy("RequireGarageOwner",
        p => p.RequireRole("GarageOwner", "SuperAdmin"));
});

// Local Storage
builder.Services.AddBlazoredLocalStorage();

// Services
builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddSingleton<ToastService>();

await builder.Build().RunAsync();
*/
