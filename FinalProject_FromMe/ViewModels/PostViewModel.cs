namespace FinalProject_FromMe.ViewModels;

public class PostViewModel
{
    public int Id { get; set; }

    public string? Text { get; set; }

    public string? ImagePath { get; set; }

    public DateTime CreatedAt { get; set; }

    public string UserName { get; set; } = string.Empty;

    public int LikeCount { get; set; }

    public bool IsLikedByCurrentUser { get; set; }

    public List<CommentViewModel> Comments { get; set; } = new List<CommentViewModel>();
}