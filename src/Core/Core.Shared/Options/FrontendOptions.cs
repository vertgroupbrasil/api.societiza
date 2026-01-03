using System.ComponentModel.DataAnnotations;

namespace Core.Shared.Options;

public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    [Required, Url]
    public string BaseUrl { get; set; } = string.Empty;
}