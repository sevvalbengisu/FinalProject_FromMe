using FinalProject_FromMe.Data;
using FinalProject_FromMe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalProject_FromMe.Controllers;

[Authorize]
public class LikeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public LikeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int postId, int eventId)
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

        var existingLike = await _context.Likes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == currentUserId);

        if (existingLike == null)
        {
            var like = new Like
            {
                PostId = postId,
                UserId = currentUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Likes.Add(like);
        }
        else
        {
            _context.Likes.Remove(existingLike);
        }

        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Event", new { id = eventId });
    }
}