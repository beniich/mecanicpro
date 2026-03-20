using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Security.Claims;
using Blazored.LocalStorage;

namespace MecaPro.Blazor.Services;

// ─────────────────────────────────────────────────────────────
// AUTH STATE PROVIDER
// ─────────────────────────────────────────────────────────────

public class AuthStateProvider(ILocalStorageService storage, HttpClient http) : AuthenticationStateProvider
{
    private ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await storage.GetItemAsync<string>("access_token");
            if (string.IsNullOrEmpty(token))
                return new AuthenticationState(_anonymous);

            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return new AuthenticationState(_anonymous);
        }
    }

    public async Task<bool> TryRefreshAsync()
    {
        // TODO: implémenter le refresh token côté API
        return false;
    }

    public async Task LoginAsync(string accessToken, string refreshToken)
    {
        await storage.SetItemAsync("access_token", accessToken);
        await storage.SetItemAsync("refresh_token", refreshToken);
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task LogoutAsync()
    {
        await storage.RemoveItemAsync("access_token");
        await storage.RemoveItemAsync("refresh_token");
        http.DefaultRequestHeaders.Authorization = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        try
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
            if (keyValuePairs == null) return claims;

            foreach (var (key, value) in keyValuePairs)
            {
                if (key == "role" || key == "roles")
                {
                    // handle array of roles
                    var roleVal = value.ToString()!;
                    claims.Add(new Claim(ClaimTypes.Role, roleVal));
                }
                else
                {
                    claims.Add(new Claim(key, value?.ToString() ?? ""));
                }
            }

            // Map standard JWT sub -> NameIdentifier
            var sub = keyValuePairs.GetValueOrDefault("sub")?.ToString();
            if (!string.IsNullOrEmpty(sub)) claims.Add(new Claim(ClaimTypes.NameIdentifier, sub));

            var email = keyValuePairs.GetValueOrDefault("email")?.ToString();
            if (!string.IsNullOrEmpty(email)) claims.Add(new Claim(ClaimTypes.Name, email));
        }
        catch { }
        return claims;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}

// ─────────────────────────────────────────────────────────────
// API CLIENT
// ─────────────────────────────────────────────────────────────

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
        if (!res.IsSuccessStatusCode) return default;
        return await res.Content.ReadFromJsonAsync<T>();
    }

    public async Task<TResponse?> PostAsync<TResponse>(string url, object body)
    {
        var res = await http.PostAsJsonAsync(url, body);
        if (!res.IsSuccessStatusCode) return default;
        return await res.Content.ReadFromJsonAsync<TResponse>();
    }

    public async Task<TResponse?> PutAsync<TResponse>(string url, object body)
    {
        var res = await http.PutAsJsonAsync(url, body);
        if (!res.IsSuccessStatusCode) return default;
        return await res.Content.ReadFromJsonAsync<TResponse>();
    }

    public async Task<bool> DeleteAsync(string url)
    {
        var res = await http.DeleteAsync(url);
        return res.IsSuccessStatusCode;
    }

    public async Task<byte[]?> GetBytesAsync(string url) => await http.GetByteArrayAsync(url);
}

// ─────────────────────────────────────────────────────────────
// AUTH SERVICE (FRONTEND)
// ─────────────────────────────────────────────────────────────

public class AuthService(HttpClient http, AuthStateProvider authState)
{
    public async Task<(bool Success, string? Error)> LoginAsync(string email, string password)
    {
        var res = await http.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        if (!res.IsSuccessStatusCode) return (false, "Identifiants invalides.");
        var auth = await res.Content.ReadFromJsonAsync<LoginResponseDto>();
        if (auth?.AccessToken == null) return (false, "Réponse invalide du serveur.");
        await authState.LoginAsync(auth.AccessToken, auth.RefreshToken ?? "");
        return (true, null);
    }
}

// ─────────────────────────────────────────────────────────────
// DTOs FRONTEND — alignés sur Application.Common DTOs
// Ces records sont des miroirs des DTOs backend, sérialisés via JSON
// ─────────────────────────────────────────────────────────────

// Auth DTOs — alignés avec Auth.Security.cs (AuthResponseDto & LoginResponseDto)
public record LoginResponseDto(bool Requires2Fa, string? TempToken, string? AccessToken, string? RefreshToken);
public record AuthResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresAt, string UserId, string Email, string FirstName, string LastName, List<string> Roles);

// Data DTOs — miroirs exacts de Application.Common
public record DashboardStatsDto(int VehiclesInProgress, int ActiveDiagnostics, int TotalClients, int TodayRevisions);
public record VehicleDto(Guid Id, string LicensePlate, string? VIN, string Make, string Model, int Year, int Mileage, string? FuelType, string? Color, string Status, string QrCodeToken, DateTime CreatedAt);
public record VehicleDetailDto(Guid Id, string LicensePlate, string? VIN, string Make, string Model, int Year, int Mileage, string? FuelType, string? Color, string Status, string QrCodeToken, DateTime CreatedAt, string? CustomerName);
public record RevisionDto(Guid Id, Guid VehicleId, string Type, DateTime ScheduledDate, string Status, decimal EstimatedCost, int EstimatedDuration);
public record InvoiceDto(Guid Id, string Number, decimal Amount, DateTime Date, string Status, string? PdfUrl);
public record UserProfileDto(string Id, string Name, string Email, string Role, string? Avatar, string? GarageId);
public record PartDto(Guid Id, string Reference, string Name, string Category, string? Brand, decimal UnitPrice, int StockQuantity, bool IsLowStock);
public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize);
public record QrCodeDto(string Token, string Url, string Image, string LicensePlate);
public record DiagnosticDto(Guid Id, Guid VehicleId, string FaultCode, string Description, string Severity, string Status, DateTime CreatedAt);
    public record CustomerDto(Guid Id, string FirstName, string LastName, string Email, string? Phone, string Segment, int LoyaltyPoints, DateTime CreatedAt, bool IsBusiness = false, string? CompanyName = null);
    
    public record CustomerDetailDto(
        Guid Id, string FirstName, string LastName, string Email, string? Phone, string? Street, string? City, string? PostalCode,
        string Segment, int LoyaltyPoints, string? Notes, string? Tags, string PreferredContact, DateTime CreatedAt,
        List<VehicleDto> Vehicles, List<LoyaltyTransactionDto> LoyaltyHistory, List<RevisionDto> Revisions,
        bool IsBusiness = false, string? CompanyName = null, string? TaxId = null
    );
public record LoyaltyTransactionDto(int Points, string Reason, DateTime Date);

public record RevisionTaskDto(Guid Id, string Description, int EstimatedMinutes, int? ActualMinutes, bool IsCompleted);
public record RevisionPartDto(Guid Id, string PartName, int Quantity, decimal UnitPrice, decimal Total);

    List<RevisionTaskDto> Tasks, List<RevisionPartDto> Parts
);

public record WorkshopScheduleDto(DateTime Date, List<AppointmentDto> Appointments);
public record AppointmentDto(Guid Id, string Title, string Description, string Status, DateTime Start, int DurationMinutes, string? ResourceName);
