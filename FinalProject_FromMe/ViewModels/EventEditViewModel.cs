using System.ComponentModel.DataAnnotations;

namespace FinalProject_FromMe.ViewModels;

public class EventEditViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsPublic { get; set; }
}