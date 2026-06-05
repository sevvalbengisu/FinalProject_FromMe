namespace FinalProject_FromMe.ViewModels;

public class CommentViewModel
{
    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}