using System.Text.Json.Serialization;

namespace RePKG.WpfGui.Models;

/// <summary>
/// 映射 project.json 字段
/// </summary>
public class ProjectInfo
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("file")]
    public string? File { get; set; }

    [JsonPropertyName("preview")]
    public string? Preview { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("visibility")]
    public string? Visibility { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("workshopid")]
    public string? WorkshopId { get; set; }

    [JsonPropertyName("authorsteamid")]
    public string? AuthorSteamId { get; set; }

    [JsonPropertyName("tags")]
    public string[]? Tags { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}
