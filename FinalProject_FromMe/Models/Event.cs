using System.ComponentModel.DataAnnotations;

namespace FinalProject_FromMe.Models;

public class Event
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string OwnerId { get; set; } = string.Empty;

    public ApplicationUser? Owner { get; set; }

    public ICollection<Post> Posts { get; set; } = new List<Post>();
}