using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Jerry.Sponsors;

public sealed class SponsorsManager
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly DiscordAuthManager _discordAuthManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    private ISawmill _sawmill = default!;

    private readonly HttpClient _httpClient = new();
    private string _guildId = default!;
    private string _apiUrl = default!;
    private string _apiKey = default!;
    private bool _enabled = false;

    private Dictionary<NetUserId, SponsorData> _cachedSponsors = new();

    public void Initialize()
    {
        _configuration.OnValueChanged(CCCVars.CCCVars.DiscordGuildID, s => _guildId = s, true);
        _configuration.OnValueChanged(CCCVars.CCCVars.DiscordApiUrl, s => _apiUrl = s, true);
        _configuration.OnValueChanged(CCCVars.CCCVars.DiscordApiKey, (value) => _apiKey = value, true);
        _configuration.OnValueChanged(CCCVars.CCCVars.SponsorsEnabled, (value) => _enabled = value, true);

        _discordAuthManager.PlayerVerified += OnPlayerVerified;
        _netManager.Disconnect += OnDisconnect;

        _sawmill = Logger.GetSawmill("sponsors");
    }

    private void OnDisconnect(object? sender, NetDisconnectedArgs e)
    {
        _cachedSponsors.Remove(e.Channel.UserId);
    }

    private async void OnPlayerVerified(object? sender, ICommonSession e)
    {
        if (!_enabled)
            return;

        var roles = await GetRoles(e.UserId);
        if (roles == null)
            return;

        var isGiven = await IsGiven(e.UserId);

        var level = SponsorData.ParseRoles(roles);
        if (level == SponsorLevel.None)
            return;

        var data = new SponsorData(level, e.UserId, isGiven);
        _cachedSponsors.Add(e.UserId, data);

        _sawmill.Info($"{e.UserId} is sponsor now.\nUserId: {e.UserId}. Level: {Enum.GetName(data.Level)}:{(int)data.Level}");
    }

    private async Task<List<string>?> GetRoles(NetUserId userId)
    {
        var requestUrl = $"{_apiUrl}/roles?userid={userId}&guildid={_guildId}&api_token={_apiKey}";
        var response = await _httpClient.GetAsync(requestUrl);

        if (!response.IsSuccessStatusCode)
        {
            _sawmill.Error($"Failed to retrieve roles for user {userId}: {response.StatusCode}");
            return null;
        }

        var responseContent = await response.Content.ReadAsStringAsync();

        var rolesJson = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(responseContent);

        if (rolesJson != null && rolesJson.TryGetValue("roles", out var roles))
        {
            return roles;
        }

        _sawmill.Error($"Roles not found in response for user {userId}");
        return null;
    }

    private async Task<bool> IsGiven(NetUserId userId)
    {
        var requestUrl = $"{_apiUrl}/is_given?userid={userId}&api_token={_apiKey}";
        var response = await _httpClient.GetAsync(requestUrl);

        return (int)response.StatusCode == 200;
    }

    public async Task SetGiven(NetUserId userId, bool given)
    {
        var requestUrl = $"{_apiUrl}/given?userid={userId}&given={(given ? 1 : 0)}&api_token={_apiKey}";
        var response = await _httpClient.PostAsync(requestUrl, null);
        if (!response.IsSuccessStatusCode)
            _sawmill.Error($"Error setting given value for {userId}");
    }

    public async Task MakeWipe()
    {
        var requestUrl = $"{_apiUrl}/wipe_given?api_token={_apiKey}";
        var response = await _httpClient.PostAsync(requestUrl, null);
        if (!response.IsSuccessStatusCode)
            _sawmill.Error("Error wiping given records.");

        foreach (var data in _cachedSponsors)
        {
            data.Value.IsGiven = false;
        }
    }

    public bool TryGetSponsorData(NetUserId userId, [NotNullWhen(true)] out SponsorData? sponsorData)
    {
        return _cachedSponsors.TryGetValue(userId, out sponsorData);
    }
}

public sealed class SponsorData
{
    public static readonly Dictionary<string, SponsorLevel> RolesMap = new()
    {
        { "1150745050196738169", SponsorLevel.Normal },
        { "1150488411447242844", SponsorLevel.Lord }
    };

    public static SponsorLevel ParseRoles(List<string> roles)
    {
        var highestRole = SponsorLevel.None;
        foreach (var role in roles)
        {
            if ((int)RolesMap[role] > (int)highestRole)
                highestRole = RolesMap[role];
        }

        return highestRole;
    }

    public SponsorData(SponsorLevel level, NetUserId userId, bool given)
    {
        Level = level;
        UserId = userId;
        IsGiven = given;
    }

    public SponsorLevel Level;
    public NetUserId UserId;
    public bool IsGiven;
}

public enum SponsorLevel
{
    None = 0,
    Normal = 1,
    Lord = 2,
}
