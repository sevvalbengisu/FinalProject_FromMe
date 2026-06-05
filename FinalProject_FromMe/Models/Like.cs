using System.ComponentModel.DataAnnotations;

namespace FinalProject_FromMe.Models;

public class Like
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int PostId { get; set; }
    public Post? Post { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }
}