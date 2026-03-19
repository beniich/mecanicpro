// ============================================================
// .NET MAUI MOBILE APP — MecaPro Mobile
// Pages: Login, Dashboard, Scanner QR, Fiche Véhicule,
//        Chat, Notifications, Révisions
// ============================================================

// ─────────────────────────────────────────────────────────────
// MauiProgram.cs
// ─────────────────────────────────────────────────────────────

using CommunityToolkit.Maui;
using MecaPro.Mobile.Services;
using MecaPro.Mobile.ViewModels;
using ZXing.Net.Maui.Controls;

namespace MecaPro.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Barlow-Regular.ttf", "Barlow");
                fonts.AddFont("Barlow-Bold.ttf", "BarlowBold");
                fonts.AddFont("Barlow-SemiBold.ttf", "BarlowSemiBold");
                fonts.AddFont("SpaceMono-Regular.ttf", "Mono");
            });

        // HTTP Client
        builder.Services.AddHttpClient("MecaProApi", client =>
        {
            client.BaseAddress = new Uri("https://api.mecapro.app/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // Services
        builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<AuthMobileService>();
        builder.Services.AddSingleton<ChatMobileService>();
        builder.Services.AddSingleton<NotificationMobileService>();

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<ScannerViewModel>();
        builder.Services.AddTransient<VehicleDetailViewModel>();
        builder.Services.AddTransient<RevisionViewModel>();
        builder.Services.AddTransient<ChatViewModel>();

        // Pages
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ScannerPage>();
        builder.Services.AddTransient<VehicleDetailPage>();
        builder.Services.AddTransient<RevisionPage>();
        builder.Services.AddTransient<ChatPage>();
        builder.Services.AddTransient<NotificationsPage>();

        // Push notifications (Firebase)
        builder.Services.AddSingleton<IFirebaseMessaging, FirebaseMessagingService>();

        return builder.Build();
    }
}

// ─────────────────────────────────────────────────────────────
// App.xaml.cs
// ─────────────────────────────────────────────────────────────

public partial class App : Application
{
    private readonly AuthMobileService _auth;

    public App(AuthMobileService auth)
    {
        _auth = auth;
        InitializeComponent();
        SetMainPage();
    }

    private void SetMainPage()
    {
        if (_auth.IsAuthenticated)
            MainPage = new AppShell();
        else
            MainPage = new NavigationPage(new LoginPage());
    }
}

// ─────────────────────────────────────────────────────────────
// AppShell.xaml.cs
// ─────────────────────────────────────────────────────────────

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("vehicle-detail", typeof(VehicleDetailPage));
        Routing.RegisterRoute("revision-detail", typeof(RevisionPage));
        Routing.RegisterRoute("chat-conversation", typeof(ChatPage));
    }
}

/*
AppShell.xaml:
<Shell>
    <Shell.FlyoutHeader>
        <StackLayout BackgroundColor="#0d1018" Padding="20">
            <Label Text="MecaPro" FontFamily="BarlowBold" FontSize="24"
                   TextColor="#f5a623" />
        </StackLayout>
    </Shell.FlyoutHeader>

    <TabBar>
        <Tab Title="Dashboard" Icon="dashboard.png">
            <ShellContent ContentTemplate="{DataTemplate local:DashboardPage}" />
        </Tab>
        <Tab Title="Scanner" Icon="qr.png">
            <ShellContent ContentTemplate="{DataTemplate local:ScannerPage}" />
        </Tab>
        <Tab Title="Chat" Icon="chat.png">
            <ShellContent ContentTemplate="{DataTemplate local:ChatListPage}" />
        </Tab>
        <Tab Title="Révisions" Icon="calendar.png">
            <ShellContent ContentTemplate="{DataTemplate local:RevisionListPage}" />
        </Tab>
        <Tab Title="Profil" Icon="profile.png">
            <ShellContent ContentTemplate="{DataTemplate local:ProfilePage}" />
        </Tab>
    </TabBar>
</Shell>
*/

// ─────────────────────────────────────────────────────────────
// VIEWMODEL BASE
// ─────────────────────────────────────────────────────────────

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private string _errorMessage = "";

    protected async Task ExecuteAsync(Func<Task> action)
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            ErrorMessage = "";
            await action();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

// ─────────────────────────────────────────────────────────────
// LOGIN PAGE + VIEWMODEL
// ─────────────────────────────────────────────────────────────

public partial class LoginViewModel : BaseViewModel
{
    private readonly AuthMobileService _auth;
    private readonly INavigationService _nav;

    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private bool _showPassword;
    [ObservableProperty] private bool _isLogin2FA;
    [ObservableProperty] private string _otpCode = "";
    [ObservableProperty] private string _tempToken = "";
    [ObservableProperty] private bool _rememberMe = true;

    public LoginViewModel(AuthMobileService auth, INavigationService nav)
    { _auth = auth; _nav = nav; Title = "Connexion"; }

    [RelayCommand]
    private async Task LoginAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _auth.LoginAsync(Email, Password);
            if (result.Success)
                await Shell.Current.GoToAsync("//dashboard");
            else if (result.Need2Fa)
            {
                TempToken = result.TempToken!;
                IsLogin2FA = true;
            }
            else
                await Shell.Current.DisplayAlert("Erreur", result.ErrorMessage ?? "Connexion échouée", "OK");
        });
    }

    [RelayCommand]
    private async Task Verify2FAAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _auth.Verify2FAAsync(TempToken, OtpCode);
            if (result.Success)
                await Shell.Current.GoToAsync("//dashboard");
            else
            {
                OtpCode = "";
                await Shell.Current.DisplayAlert("Erreur", "Code invalide", "OK");
            }
        });
    }

    [RelayCommand]
    private async Task BiometricLoginAsync()
    {
        var token = await _auth.GetSavedTokenAsync();
        if (token == null) return;

        // Try biometric auth
        var authenticated = await BiometricAuthService.AuthenticateAsync("Accès MecaPro");
        if (authenticated)
            await Shell.Current.GoToAsync("//dashboard");
    }
}

/*
LoginPage.xaml:
<ContentPage BackgroundColor="#07090e">
    <ScrollView>
        <StackLayout Padding="32,60,32,32">
            <Label Text="⬡ MecaPro" FontFamily="BarlowBold" FontSize="28"
                   TextColor="#f5a623" HorizontalOptions="Center" Margin="0,0,0,8" />
            <Label Text="Espace Mécanicien" FontFamily="Barlow" FontSize="14"
                   TextColor="#6b7d96" HorizontalOptions="Center" Margin="0,0,0,40" />

            <Entry x:Name="EmailEntry" Placeholder="Email" Text="{Binding Email}"
                   Keyboard="Email" BackgroundColor="#121720" TextColor="White"
                   PlaceholderColor="#334155" />

            <Grid>
                <Entry x:Name="PwdEntry" Placeholder="Mot de passe" Text="{Binding Password}"
                       IsPassword="{Binding ShowPassword, Converter={x:Static BoolNegateConverter.Default}}"
                       BackgroundColor="#121720" TextColor="White" />
                <ImageButton Command="{Binding TogglePasswordCommand}" HorizontalOptions="End" />
            </Grid>

            <Button Text="Se connecter" Command="{Binding LoginCommand}"
                    BackgroundColor="#f5a623" TextColor="#07090e"
                    FontFamily="BarlowBold" CornerRadius="8" />

            <Button Text="🔐 Connexion biométrique" Command="{Binding BiometricLoginCommand}"
                    BackgroundColor="Transparent" TextColor="#6b7d96" />
        </StackLayout>
    </ScrollView>
</ContentPage>
*/

// ─────────────────────────────────────────────────────────────
// DASHBOARD VIEWMODEL
// ─────────────────────────────────────────────────────────────

public partial class DashboardViewModel : BaseViewModel
{
    private readonly ApiService _api;

    [ObservableProperty] private DashboardStatsDto? _stats;
    [ObservableProperty] private ObservableCollection<VehicleSummaryDto> _recentVehicles = new();
    [ObservableProperty] private ObservableCollection<AlertDto> _alerts = new();
    [ObservableProperty] private ObservableCollection<RevisionSummaryDto> _todayRevisions = new();

    public DashboardViewModel(ApiService api) { _api = api; Title = "Dashboard"; }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        await ExecuteAsync(async () =>
        {
            Stats = await _api.GetAsync<DashboardStatsDto>("/api/v1/dashboard/stats");

            var vehicles = await _api.GetAsync<PagedResult<VehicleSummaryDto>>("/api/v1/vehicles?pageSize=5");
            RecentVehicles = new ObservableCollection<VehicleSummaryDto>(vehicles?.Items ?? []);

            var alerts = await _api.GetAsync<List<AlertDto>>("/api/v1/dashboard/alerts");
            Alerts = new ObservableCollection<AlertDto>(alerts ?? []);

            var revisions = await _api.GetAsync<List<RevisionSummaryDto>>("/api/v1/revisions?date=today");
            TodayRevisions = new ObservableCollection<RevisionSummaryDto>(revisions ?? []);
        });
    }

    [RelayCommand]
    private async Task GoToScannerAsync()
        => await Shell.Current.GoToAsync("//scanner");

    [RelayCommand]
    private async Task ViewVehicleAsync(Guid vehicleId)
        => await Shell.Current.GoToAsync($"vehicle-detail?id={vehicleId}");
}

// ─────────────────────────────────────────────────────────────
// QR SCANNER PAGE + VIEWMODEL
// ─────────────────────────────────────────────────────────────

public partial class ScannerViewModel : BaseViewModel
{
    private readonly ApiService _api;

    [ObservableProperty] private bool _isDetecting = true;
    [ObservableProperty] private string? _lastScannedCode;
    [ObservableProperty] private VehicleDetailDto? _scannedVehicle;
    [ObservableProperty] private bool _showResult;

    public ScannerViewModel(ApiService api) { _api = api; Title = "Scanner QR"; }

    public async Task OnBarcodeDetected(string code)
    {
        if (!IsDetecting || code == LastScannedCode) return;
        IsDetecting = false;
        LastScannedCode = code;

        // Extract token from URL
        var token = code.Contains("/v/")
            ? code.Split("/v/").Last()
            : code;

        await ExecuteAsync(async () =>
        {
            ScannedVehicle = await _api.GetAsync<VehicleDetailDto>($"/api/v1/vehicles/by-qr/{token}");
            ShowResult = ScannedVehicle != null;
            if (ScannedVehicle == null)
                await Shell.Current.DisplayAlert("Introuvable", "QR code non reconnu.", "OK");
        });
    }

    [RelayCommand]
    private async Task ViewVehicleAsync()
    {
        if (ScannedVehicle == null) return;
        ShowResult = false;
        IsDetecting = true;
        await Shell.Current.GoToAsync($"vehicle-detail?id={ScannedVehicle.Id}");
    }

    [RelayCommand]
    private void RescanAsync()
    {
        ShowResult = false;
        ScannedVehicle = null;
        LastScannedCode = null;
        IsDetecting = true;
    }
}

/*
ScannerPage.xaml:
<ContentPage BackgroundColor="#07090e">
    <Grid>
        <zxing:CameraBarcodeReaderView
            x:Name="Scanner"
            IsDetecting="{Binding IsDetecting}"
            BarcodesDetected="OnBarcodesDetected"
            Options="{Binding ScanOptions}" />

        <!-- Overlay frame -->
        <Frame BackgroundColor="Transparent" BorderColor="#f5a623"
               WidthRequest="250" HeightRequest="250"
               HorizontalOptions="Center" VerticalOptions="Center" />

        <Label Text="Pointez vers le QR Code du véhicule"
               TextColor="White" HorizontalOptions="Center"
               VerticalOptions="End" Margin="20" />

        <!-- Result bottom sheet -->
        <Frame IsVisible="{Binding ShowResult}" VerticalOptions="End"
               BackgroundColor="#1d2538" CornerRadius="20">
            <StackLayout Padding="20">
                <Label Text="{Binding ScannedVehicle.LicensePlate}"
                       FontFamily="Mono" TextColor="#f5a623" FontSize="22" />
                <Label Text="{Binding ScannedVehicle.MakeModelYear}"
                       TextColor="White" FontFamily="BarlowBold" />
                <Label Text="{Binding ScannedVehicle.CustomerName}"
                       TextColor="#6b7d96" />
                <Button Text="Voir la fiche complète"
                        Command="{Binding ViewVehicleCommand}"
                        BackgroundColor="#f5a623" TextColor="#07090e" />
                <Button Text="Rescanner" Command="{Binding RescanCommand}"
                        BackgroundColor="Transparent" TextColor="#6b7d96" />
            </StackLayout>
        </Frame>
    </Grid>
</ContentPage>
*/

// ─────────────────────────────────────────────────────────────
// CHAT VIEWMODEL (SignalR mobile)
// ─────────────────────────────────────────────────────────────

public partial class ChatViewModel : BaseViewModel
{
    private readonly ChatMobileService _chat;
    private readonly ApiService _api;

    [ObservableProperty] private ObservableCollection<ChatMessageDto> _messages = new();
    [ObservableProperty] private ObservableCollection<ConversationDto> _conversations = new();
    [ObservableProperty] private string _messageText = "";
    [ObservableProperty] private ConversationDto? _activeConversation;
    [ObservableProperty] private bool _isTyping;

    public ChatViewModel(ChatMobileService chat, ApiService api)
    { _chat = chat; _api = api; Title = "Chat"; }

    public async Task InitializeAsync()
    {
        await _chat.ConnectAsync();
        _chat.OnMessageReceived += OnMessageReceived;
        Conversations = new ObservableCollection<ConversationDto>(
            await _api.GetAsync<List<ConversationDto>>("/api/v1/chat/conversations") ?? []);
    }

    [RelayCommand]
    private async Task SelectConversationAsync(ConversationDto conv)
    {
        ActiveConversation = conv;
        var history = await _api.GetAsync<List<ChatMessageDto>>($"/api/v1/chat/history/{conv.UserId}");
        Messages = new ObservableCollection<ChatMessageDto>(history ?? []);
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageText) || ActiveConversation == null) return;
        var content = MessageText.Trim();
        MessageText = "";
        await _chat.SendAsync(ActiveConversation.UserId, content);
        Messages.Add(new ChatMessageDto { Content = content, IsOwn = true, SentAt = DateTime.Now });
    }

    private void OnMessageReceived(ChatMessageDto msg)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (msg.SenderId == ActiveConversation?.UserId)
                Messages.Add(msg);
        });
    }

    public override void Dispose()
    {
        _chat.OnMessageReceived -= OnMessageReceived;
    }
}

// ─────────────────────────────────────────────────────────────
// API SERVICE MOBILE
// ─────────────────────────────────────────────────────────────

public class ApiService
{
    private readonly HttpClient _http;
    private readonly ISecureStorageService _storage;

    public ApiService(IHttpClientFactory factory, ISecureStorageService storage)
    { _http = factory.CreateClient("MecaProApi"); _storage = storage; }

    public async Task<T?> GetAsync<T>(string url)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync(url);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            if (await RefreshTokenAsync())
                response = await _http.GetAsync(url);
            else return default;
        }
        if (!response.IsSuccessStatusCode) return default;
        var json = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<T>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<T?> PostAsync<T>(string url, object? body)
    {
        await SetAuthHeaderAsync();
        var content = body != null
            ? new StringContent(System.Text.Json.JsonSerializer.Serialize(body),
                System.Text.Encoding.UTF8, "application/json")
            : null;
        var response = await _http.PostAsync(url, content);
        if (!response.IsSuccessStatusCode) return default;
        var json = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<T>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await _storage.GetAsync("access_token");
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<bool> RefreshTokenAsync()
    {
        var refreshToken = await _storage.GetAsync("refresh_token");
        if (string.IsNullOrEmpty(refreshToken)) return false;
        var response = await _http.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        if (!response.IsSuccessStatusCode) return false;
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        if (auth == null) return false;
        await _storage.SetAsync("access_token", auth.AccessToken);
        await _storage.SetAsync("refresh_token", auth.RefreshToken);
        await SetAuthHeaderAsync();
        return true;
    }
}

// ─────────────────────────────────────────────────────────────
// AUTH MOBILE SERVICE
// ─────────────────────────────────────────────────────────────

public class AuthMobileService
{
    private readonly HttpClient _http;
    private readonly ISecureStorageService _storage;

    public bool IsAuthenticated => !string.IsNullOrEmpty(
        Task.Run(() => _storage.GetAsync("access_token")).GetAwaiter().GetResult());

    public AuthMobileService(IHttpClientFactory factory, ISecureStorageService storage)
    { _http = factory.CreateClient("MecaProApi"); _storage = storage; }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("/api/v1/auth/login",
            new { email, password });
        if (!response.IsSuccessStatusCode) return AuthResult.Fail("Identifiants incorrects.");
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        if (result == null) return AuthResult.Fail("Erreur serveur.");
        if (result.Requires2Fa) return AuthResult.Requires2Fa(result.TempToken!);
        await SaveTokensAsync(new AuthResponseDto(
            result.AccessToken!, result.RefreshToken!, DateTime.Now.AddMinutes(15),
            "", "", "", "", new()));
        return AuthResult.Ok();
    }

    public async Task<AuthResult> Verify2FAAsync(string tempToken, string code)
    {
        var response = await _http.PostAsJsonAsync("/api/v1/auth/2fa/verify",
            new { tempToken, code });
        if (!response.IsSuccessStatusCode) return AuthResult.Fail("Code invalide.");
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        await SaveTokensAsync(auth!);
        return AuthResult.Ok();
    }

    private async Task SaveTokensAsync(AuthResponseDto auth)
    {
        await _storage.SetAsync("access_token", auth.AccessToken);
        await _storage.SetAsync("refresh_token", auth.RefreshToken);
    }

    public async Task<string?> GetSavedTokenAsync()
        => await _storage.GetAsync("access_token");

    public async Task LogoutAsync()
    {
        await _storage.RemoveAsync("access_token");
        await _storage.RemoveAsync("refresh_token");
    }
}

// ─────────────────────────────────────────────────────────────
// CHAT MOBILE SERVICE (SignalR)
// ─────────────────────────────────────────────────────────────

public class ChatMobileService
{
    private HubConnection? _hub;
    private readonly ISecureStorageService _storage;

    public event Action<ChatMessageDto>? OnMessageReceived;
    public event Action<string>? OnTyping;

    public ChatMobileService(ISecureStorageService storage) => _storage = storage;

    public async Task ConnectAsync()
    {
        var token = await _storage.GetAsync("access_token");
        _hub = new HubConnectionBuilder()
            .WithUrl($"https://api.mecapro.app/hubs/chat?access_token={token}")
            .WithAutomaticReconnect()
            .Build();

        _hub.On<ChatMessageDto>("ReceiveMessage", msg => OnMessageReceived?.Invoke(msg));
        _hub.On<string>("UserTyping", userId => OnTyping?.Invoke(userId));
        await _hub.StartAsync();
    }

    public async Task SendAsync(string recipientId, string content, string? vehicleId = null)
    {
        if (_hub?.State == HubConnectionState.Connected)
            await _hub.InvokeAsync("SendMessage", recipientId, content, vehicleId);
    }

    public async Task DisconnectAsync()
    {
        if (_hub != null) await _hub.StopAsync();
    }
}

// ─────────────────────────────────────────────────────────────
// SECURE STORAGE SERVICE
// ─────────────────────────────────────────────────────────────

public interface ISecureStorageService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task RemoveAsync(string key);
}

public class SecureStorageService : ISecureStorageService
{
    public async Task<string?> GetAsync(string key)
    {
        try { return await SecureStorage.Default.GetAsync(key); }
        catch { return null; }
    }

    public async Task SetAsync(string key, string value)
        => await SecureStorage.Default.SetAsync(key, value);

    public Task RemoveAsync(string key)
    {
        SecureStorage.Default.Remove(key);
        return Task.CompletedTask;
    }
}

// ─────────────────────────────────────────────────────────────
// FIREBASE PUSH NOTIFICATIONS
// ─────────────────────────────────────────────────────────────

public class NotificationMobileService
{
    private readonly ApiService _api;

    public NotificationMobileService(ApiService api) => _api = api;

    public async Task RegisterPushTokenAsync()
    {
        // Get FCM token
        var token = await GetFCMTokenAsync();
        if (string.IsNullOrEmpty(token)) return;

        // Register with backend
        await _api.PostAsync<object>("/api/v1/notifications/push-token", new
        {
            token,
            platform = DeviceInfo.Current.Platform == DevicePlatform.Android ? "android" : "ios"
        });
    }

    private async Task<string?> GetFCMTokenAsync()
    {
        // Platform-specific FCM token retrieval
        return await Task.FromResult<string?>(null); // Implement per platform
    }

    public void HandlePushNotification(string title, string body, string? actionUrl)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var confirmed = await Shell.Current.DisplayAlert(title, body, "Voir", "Ignorer");
            if (confirmed && !string.IsNullOrEmpty(actionUrl))
                await Shell.Current.GoToAsync(actionUrl);
        });
    }
}

// ─────────────────────────────────────────────────────────────
// MAUI STYLES (MecaPro dark theme)
// ─────────────────────────────────────────────────────────────

/*
Resources/Styles/MecaProStyles.xaml:

<ResourceDictionary>
    <!-- Colors -->
    <Color x:Key="PageBg">#07090e</Color>
    <Color x:Key="CardBg">#1d2538</Color>
    <Color x:Key="SurfaceBg">#121720</Color>
    <Color x:Key="Amber">#f5a623</Color>
    <Color x:Key="TextPrimary">#dde4f0</Color>
    <Color x:Key="TextSecondary">#6b7d96</Color>
    <Color x:Key="BorderColor">#232e42</Color>
    <Color x:Key="GreenAccent">#22c55e</Color>
    <Color x:Key="RedAccent">#ef4444</Color>
    <Color x:Key="BlueAccent">#3b82f6</Color>

    <!-- Button Primary -->
    <Style x:Key="PrimaryButton" TargetType="Button">
        <Setter Property="BackgroundColor" Value="{StaticResource Amber}" />
        <Setter Property="TextColor" Value="{StaticResource PageBg}" />
        <Setter Property="FontFamily" Value="BarlowBold" />
        <Setter Property="CornerRadius" Value="8" />
        <Setter Property="HeightRequest" Value="48" />
    </Style>

    <!-- Card Frame -->
    <Style x:Key="CardFrame" TargetType="Frame">
        <Setter Property="BackgroundColor" Value="{StaticResource CardBg}" />
        <Setter Property="BorderColor" Value="{StaticResource BorderColor}" />
        <Setter Property="CornerRadius" Value="12" />
        <Setter Property="Padding" Value="16" />
    </Style>

    <!-- Plate Label -->
    <Style x:Key="PlateLabel" TargetType="Label">
        <Setter Property="FontFamily" Value="Mono" />
        <Setter Property="TextColor" Value="{StaticResource Amber}" />
        <Setter Property="FontSize" Value="16" />
        <Setter Property="FontAttributes" Value="Bold" />
    </Style>

    <!-- KPI Value -->
    <Style x:Key="KpiValue" TargetType="Label">
        <Setter Property="FontFamily" Value="BarlowBold" />
        <Setter Property="FontSize" Value="28" />
        <Setter Property="TextColor" Value="{StaticResource TextPrimary}" />
    </Style>
</ResourceDictionary>
*/

// ─────────────────────────────────────────────────────────────
// VEHICLE DETAIL PAGE (MAUI)
// ─────────────────────────────────────────────────────────────

public partial class VehicleDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ApiService _api;
    private Guid _vehicleId;

    [ObservableProperty] private VehicleDetailDto? _vehicle;
    [ObservableProperty] private ObservableCollection<DiagnosticDto> _diagnostics = new();
    [ObservableProperty] private ObservableCollection<RevisionDto> _revisions = new();

    public VehicleDetailViewModel(ApiService api) { _api = api; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id))
            _vehicleId = Guid.Parse(id.ToString()!);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        await ExecuteAsync(async () =>
        {
            Vehicle = await _api.GetAsync<VehicleDetailDto>($"/api/v1/vehicles/{_vehicleId}");
            if (Vehicle != null)
            {
                Title = $"{Vehicle.Make} {Vehicle.Model}";
                Diagnostics = new ObservableCollection<DiagnosticDto>(Vehicle.ActiveDiagnostics);
                Revisions = new ObservableCollection<RevisionDto>(Vehicle.RevisionHistory.Take(5));
            }
        });
    }

    [RelayCommand]
    private async Task AddDiagnosticAsync()
        => await Shell.Current.GoToAsync($"add-diagnostic?vehicleId={_vehicleId}");

    [RelayCommand]
    private async Task ScheduleRevisionAsync()
        => await Shell.Current.GoToAsync($"revision-detail?vehicleId={_vehicleId}");

    [RelayCommand]
    private async Task ChatWithClientAsync()
    {
        if (Vehicle?.CustomerId != null)
            await Shell.Current.GoToAsync($"chat-conversation?userId={Vehicle.CustomerId}");
    }

    [RelayCommand]
    private async Task CallClientAsync()
    {
        if (Vehicle?.CustomerPhone != null)
            await Launcher.OpenAsync($"tel:{Vehicle.CustomerPhone}");
    }
}

// ─────────────────────────────────────────────────────────────
// MAUI.csproj
// ─────────────────────────────────────────────────────────────

/*
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0-android;net8.0-ios</TargetFrameworks>
    <RootNamespace>MecaPro.Mobile</RootNamespace>
    <ApplicationId>app.mecapro.mobile</ApplicationId>
    <ApplicationVersion>1</ApplicationVersion>
    <ApplicationDisplayVersion>1.0.0</ApplicationDisplayVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Maui.Controls" Version="8.0.80" />
    <PackageReference Include="CommunityToolkit.Maui" Version="7.0.1" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
    <PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.3" />
    <PackageReference Include="ZXing.Net.Maui.Controls" Version="0.4.0" />
    <PackageReference Include="Plugin.Firebase.CloudMessaging" Version="2.0.4" />
    <PackageReference Include="Microcharts.Maui" Version="1.0.0" />
  </ItemGroup>
</Project>
*/
