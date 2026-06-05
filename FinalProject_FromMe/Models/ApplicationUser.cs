using Microsoft.AspNetCore.Identity;

namespace FinalProject_FromMe.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<Event> Events { get; set; } = new List<Event>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}