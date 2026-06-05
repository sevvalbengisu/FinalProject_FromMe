namespace FinalProject_FromMe.ViewModels;

public class ExploreEventViewModel
{
    public int EventId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string OwnerName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime LastActivityAt { get; set; }

    public int PostCount { get; set; }
}