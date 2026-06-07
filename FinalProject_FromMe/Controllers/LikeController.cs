using System.Security.Claims;
using FinalProject_FromMe.Data;
using FinalProject_FromMe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalProject_FromMe.Controllers;

[Authorize]
public class LikeController : Controller
{
    private readonly ApplicationDbContext _context;

    public LikeController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int postId, int eventId, int returnScroll = 0)
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

        var existingLike = await _context.Likes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == currentUserId);

        if (existingLike == null)
        {
            var like = new Like
            {
                PostId = postId,
                UserId = currentUserId
            };

            _context.Likes.Add(like);
        }
        else
        {
            _context.Likes.Remove(existingLike);
        }

        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Event", new { id = eventId, scroll = returnScroll });
    }
}