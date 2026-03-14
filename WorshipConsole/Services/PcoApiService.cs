using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WorshipConsole.Models;

namespace WorshipConsole.Services;

public class PcoApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PcoApiService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PcoApiService(HttpClient httpClient, IConfiguration configuration, ILogger<PcoApiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        var appId = _configuration["Pco:AppId"];
        var appSecret = _configuration["Pco:AppSecret"];

        if (!string.IsNullOrWhiteSpace(appId) && !string.IsNullOrWhiteSpace(appSecret))
        {
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{appId}:{appSecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

        _httpClient.BaseAddress = new Uri("https://api.planningcenteronline.com");
    }

    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(_configuration["Pco:AppId"])
            && !string.IsNullOrWhiteSpace(_configuration["Pco:AppSecret"])
            && !string.IsNullOrWhiteSpace(_configuration["Pco:ServiceTypeId"]);
    }

    public async Task<ContactInfo?> GetNextSaturdayContactsAsync()
    {
        var serviceTypeId = _configuration["Pco:ServiceTypeId"];
        if (string.IsNullOrWhiteSpace(serviceTypeId))
        {
            _logger.LogWarning("Pco:ServiceTypeId is not configured.");
            return null;
        }

        var plan = await GetNextSaturdayPlanAsync(serviceTypeId);
        if (plan == null)
        {
            _logger.LogWarning("No upcoming Saturday plan found for service type {ServiceTypeId}.", serviceTypeId);
            return null;
        }

        var teamMembers = await GetPlanTeamMembersAsync(serviceTypeId, plan.Id);

        var propresenterPosition = _configuration["Pco:ProPresenterPosition"] ?? "ProPresenter";
        var livestreamPosition = _configuration["Pco:LivestreamPosition"] ?? "Livestream";
        var worshipCoordinatorPosition = _configuration["Pco:WorshipCoordinatorPosition"] ?? "Worship Coordinator";

        return new ContactInfo
        {
            ServiceDate = plan.Attributes.SortDate,
            ServiceDates = plan.Attributes.Dates,
            ProPresenter = FindTeamMember(teamMembers, propresenterPosition),
            Livestream = FindTeamMember(teamMembers, livestreamPosition),
            WorshipCoordinator = FindTeamMember(teamMembers, worshipCoordinatorPosition)
        };
    }

    private async Task<PcoPlan?> GetNextSaturdayPlanAsync(string serviceTypeId)
    {
        try
        {
            var url = $"/services/v2/service_types/{serviceTypeId}/plans?filter=future&order=sort_date&per_page=10";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PcoResponse<PcoPlan>>(json, JsonOptions);

            return result?.Data.FirstOrDefault(p => p.Attributes.SortDate.DayOfWeek == DayOfWeek.Saturday);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching plans from PCO for service type {ServiceTypeId}.", serviceTypeId);
            return null;
        }
    }

    private async Task<List<PcoPlanPerson>> GetPlanTeamMembersAsync(string serviceTypeId, string planId)
    {
        try
        {
            var url = $"/services/v2/service_types/{serviceTypeId}/plans/{planId}/team_members";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PcoResponse<PcoPlanPerson>>(json, JsonOptions);

            return result?.Data ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching team members from PCO for plan {PlanId}.", planId);
            return [];
        }
    }

    private static string? FindTeamMember(List<PcoPlanPerson> members, string positionName)
    {
        return members.FirstOrDefault(m =>
            m.Attributes.TeamPositionName != null &&
            m.Attributes.TeamPositionName.Contains(positionName, StringComparison.OrdinalIgnoreCase))
            ?.Attributes.Name;
    }
}
