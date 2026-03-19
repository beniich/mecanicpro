using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Security.Claims;
using Blazored.LocalStorage;

namespace MecaPro.Blazor.Services;

public class ApiClient(HttpClient http, AuthStateProvider authState, NavigationManager nav)
{
    public async Task<T?> GetAsync<T>(string url)
    {
        var res = await http.GetAsync(url);
        if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            if (await authState.TryRefreshAsync()) return await http.GetFromJsonAsync<T>(url);
            nav.NavigateTo("/login");
            return default;
        }
        return await res.Content.ReadFromJsonAsync<T>();
    }

    public async Task<T?> PostAsync<T>(string url, object body)
    {
        var res = await http.PostAsJsonAsync(url, body);
        return await res.Content.ReadFromJsonAsync<T>();
    }

    public async Task<byte[]?> GetBytesAsync(string url) => await http.GetByteArrayAsync(url);
}

public class AuthStateProvider(ILocalStorageService storage, HttpClient http) : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await storage.GetItemAsync<string>("access_token");
        if (string.IsNullOrEmpty(token)) return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "User") }, "jwt")));
    }

    public async Task<bool> TryRefreshAsync() => false; // Simplified for now

    public async Task LoginAsync(string token, string refresh)
    {
        await storage.SetItemAsync("access_token", token);
        await storage.SetItemAsync("refresh_token", refresh);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task LogoutAsync()
    {
        await storage.RemoveItemAsync("access_token");
        await storage.RemoveItemAsync("refresh_token");
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}

public class AuthService(HttpClient http, AuthStateProvider authState)
{
    public async Task<bool> LoginAsync(string email, string password)
    {
        var res = await http.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        if (!res.IsSuccessStatusCode) return false;
        var auth = await res.Content.ReadFromJsonAsync<AuthResponseDto>();
        if (auth == null) return false;
        await authState.LoginAsync(auth.AccessToken, auth.RefreshToken);
        return true;
    }
}

public record AuthResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);
public record DashboardStatsDto(int VehiclesInProgress, int ActiveDiagnostics, int CompletedRevisions, decimal MonthlyRevenue, int TotalClients, int LowStockParts, int TodayRevisions, int UnreadMessages);
public record VehicleSummaryDto(Guid Id, string LicensePlate, string Make, string Model, string Status, DateTime? NextRevisionDate, string CustomerName);
public record PagedResult<T>(IEnumerable<T> Items, int Total);
public record AlertDto(string Title, string Description, string Type, string ActionUrl);
public record RevisionSummaryDto(Guid Id, string Type, DateTime ScheduledDate, int EstimatedMinutes, decimal EstimatedCost, string Status, string StatusClass, string VehiclePlate, string CustomerName);
public record RevenueDataPoint(string Month, decimal Revisions, decimal Parts);
