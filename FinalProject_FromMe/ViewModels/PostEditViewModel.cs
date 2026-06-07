using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FinalProject_FromMe.ViewModels;

public class PostEditViewModel
{
    public int Id { get; set; }

    public int EventId { get; set; }

    [StringLength(1000)]
    public string? Text { get; set; }

    public string? ExistingImagePath { get; set; }

    public IFormFile? Image { get; set; }
}