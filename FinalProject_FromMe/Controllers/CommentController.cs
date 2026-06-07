using System.Security.Claims;
using FinalProject_FromMe.Data;
using FinalProject_FromMe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalProject_FromMe.Controllers;

[Authorize]
public class CommentController : Controller
{
    private readonly ApplicationDbContext _context;

    public CommentController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int postId, int eventId, string text, int returnScroll = 0)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (currentUserId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var postExists = await _context.Posts
            .AnyAsync(p => p.Id == postId && p.EventId == eventId);

        if (!postExists)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return RedirectToAction("Details", "Event", new { id = eventId, scroll = returnScroll });
        }

        var comment = new Comment
        {
            Text = text,
            CreatedAt = DateTime.UtcNow,
            PostId = postId,
            UserId = currentUserId
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Event", new { id = eventId, scroll = returnScroll });
    }
}