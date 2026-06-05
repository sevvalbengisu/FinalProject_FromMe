using FinalProject_FromMe.Data;
using FinalProject_FromMe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalProject_FromMe.Controllers;

[Authorize]
public class CommentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CommentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int postId, int eventId, string text)
    {
        var currentUserId = _userManager.GetUserId(User);

        if (currentUserId == null)
        {
            return Unauthorized();
        }

        var postExists = await _context.Posts.AnyAsync(p => p.Id == postId);

        if (!postExists)
        {
            return NotFound();
        }

        // Boş yorum kontrolü:
        // Kullanıcı boş yorum gönderdiyse yorum oluşturmayız.
        if (string.IsNullOrWhiteSpace(text))
        {
            return Redirect(Url.Action("Details", "Event", new { id = eventId }) + $"#post-{postId}");
        }

        var comment = new Comment
        {
            Text = text,
            PostId = postId,
            UserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        // Yorum eklendikten sonra sayfa ilgili postun olduğu yere geri dönsün.
        return Redirect(Url.Action("Details", "Event", new { id = eventId }) + $"#post-{postId}");
    }
}