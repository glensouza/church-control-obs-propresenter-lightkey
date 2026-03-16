using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WorshipConsole.Models;

namespace WorshipConsole.Services;

public class PcoApiService
{
    private readonly HttpClient httpClient;
    private readonly IConfiguration configuration;
    private readonly ILogger<PcoApiService> logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PcoApiService(HttpClient httpClient, IConfiguration configuration, ILogger<PcoApiService> logger)
    {
        this.httpClient = httpClient;
        this.configuration = configuration;
        this.logger = logger;

        this.ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        string? appId = this.configuration["Pco:AppId"];
        string? appSecret = this.configuration["Pco:AppSecret"];

        if (!string.IsNullOrWhiteSpace(appId) && !string.IsNullOrWhiteSpace(appSecret))
        {
            string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{appId}:{appSecret}"));
            this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

        this.httpClient.BaseAddress = new Uri("https://api.planningcenteronline.com");
    }

    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(this.configuration["Pco:AppId"])
            && !string.IsNullOrWhiteSpace(this.configuration["Pco:AppSecret"])
            && !string.IsNullOrWhiteSpace(this.configuration["Pco:ServiceTypeId"]);
    }

    public async Task<ContactInfo?> GetNextSaturdayContactsAsync()
    {
        string? serviceTypeId = this.configuration["Pco:ServiceTypeId"];
        if (string.IsNullOrWhiteSpace(serviceTypeId))
        {
            this.logger.LogWarning("Pco:ServiceTypeId is not configured.");
            return null;
        }

        PcoPlan? plan = await this.GetNextSaturdayPlanAsync(serviceTypeId);
        if (plan == null)
        {
            this.logger.LogWarning("No upcoming Saturday plan found for service type {ServiceTypeId}.", serviceTypeId);
            return null;
        }

        List<PcoPlanPerson> teamMembers = await this.GetPlanTeamMembersAsync(serviceTypeId, plan.Id);

        string propresenterPosition = this.configuration["Pco:ProPresenterPosition"] ?? "ProPresenter";
        string livestreamPosition = this.configuration["Pco:LivestreamPosition"] ?? "Livestream";
        string worshipCoordinatorPosition = this.configuration["Pco:WorshipCoordinatorPosition"] ?? "Worship Coordinator";

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
            string url = $"/services/v2/service_types/{serviceTypeId}/plans?filter=future&order=sort_date&per_page=10";
            HttpResponseMessage response = await this.httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            PcoResponse<PcoPlan>? result = JsonSerializer.Deserialize<PcoResponse<PcoPlan>>(json, JsonOptions);

            return result?.Data.FirstOrDefault(p => p.Attributes.SortDate.DayOfWeek == DayOfWeek.Saturday);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error fetching plans from PCO for service type {ServiceTypeId}.", serviceTypeId);
            throw;
        }
    }

    private async Task<List<PcoPlanPerson>> GetPlanTeamMembersAsync(string serviceTypeId, string planId)
    {
        try
        {
            string url = $"/services/v2/service_types/{serviceTypeId}/plans/{planId}/team_members";
            HttpResponseMessage response = await this.httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            PcoResponse<PcoPlanPerson>? result = JsonSerializer.Deserialize<PcoResponse<PcoPlanPerson>>(json, JsonOptions);

            return result?.Data ?? [];
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error fetching team members from PCO for plan {PlanId}.", planId);
            throw;
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
