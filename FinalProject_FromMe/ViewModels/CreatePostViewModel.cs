using Microsoft.AspNetCore.Http;

namespace FinalProject_FromMe.ViewModels;

public class CreatePostViewModel
{
    public int EventId { get; set; }

    public string? Text { get; set; }

    public IFormFile? Image { get; set; }
}