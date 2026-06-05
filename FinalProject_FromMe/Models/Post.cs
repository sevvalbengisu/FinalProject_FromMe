using System.ComponentModel.DataAnnotations;

namespace FinalProject_FromMe.Models;

public class Post
{
    public int Id { get; set; }

    [StringLength(1000)]
    public string? Text { get; set; }

    public string? ImagePath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int EventId { get; set; }
    public Event? Event { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}