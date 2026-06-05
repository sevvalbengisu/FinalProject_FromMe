namespace FinalProject_FromMe.ViewModels;

public class EventDetailViewModel
{
    public int EventId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public string OwnerName { get; set; } = string.Empty;

    public List<PostViewModel> Posts { get; set; } = new List<PostViewModel>();

    public CreatePostViewModel NewPost { get; set; } = new CreatePostViewModel();
}