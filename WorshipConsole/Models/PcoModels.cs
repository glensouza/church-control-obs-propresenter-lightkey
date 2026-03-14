using System.Text.Json.Serialization;

namespace WorshipConsole.Models;

public class PcoResponse<T>
{
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = [];
}

public class PcoPlan
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("attributes")]
    public PcoPlanAttributes Attributes { get; set; } = new();
}

public class PcoPlanAttributes
{
    [JsonPropertyName("dates")]
    public string? Dates { get; set; }

    [JsonPropertyName("sort_date")]
    public DateTime SortDate { get; set; }
}

public class PcoPlanPerson
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("attributes")]
    public PcoPlanPersonAttributes Attributes { get; set; } = new();
}

public class PcoPlanPersonAttributes
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("team_position_name")]
    public string? TeamPositionName { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("photo_thumbnail")]
    public string? PhotoThumbnail { get; set; }
}

public class ContactInfo
{
    public DateTime ServiceDate { get; set; }
    public string? ServiceDates { get; set; }
    public string? ProPresenter { get; set; }
    public string? Livestream { get; set; }
    public string? WorshipCoordinator { get; set; }
}
