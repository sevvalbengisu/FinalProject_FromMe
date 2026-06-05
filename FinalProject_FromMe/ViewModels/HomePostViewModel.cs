namespace FinalProject_FromMe.ViewModels;

public class HomePostViewModel
{
    public int PostId { get; set; }

    public string? Text { get; set; }

    public string? ImagePath { get; set; }

    public DateTime CreatedAt { get; set; }

    public string UserName { get; set; } = string.Empty;

    public int EventId { get; set; }

    public string EventTitle { get; set; } = string.Empty;

    public bool EventIsPublic { get; set; }

    public int LikeCount { get; set; }

    public int CommentCount { get; set; }
}