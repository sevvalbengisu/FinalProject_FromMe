using System.Security.Claims;
using FinalProject_FromMe.Data;
using FinalProject_FromMe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalProject_FromMe.Controllers;

[Authorize]
public class PostController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public PostController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int eventId, string? text, IFormFile? image, int returnScroll = 0)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (currentUserId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var eventExists = await _context.Events.AnyAsync(e => e.Id == eventId);

        if (!eventExists)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(text) && image == null)
        {
            return RedirectToAction("Details", "Event", new { id = eventId, scroll = returnScroll });
        }

        string? imagePath = null;

        if (image != null && image.Length > 0)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "posts");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileExtension = Path.GetExtension(image.FileName);
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var fullPath = Path.Combine(uploadsFolder, fileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            imagePath = $"/uploads/posts/{fileName}";
        }

        var post = new Post
        {
            Text = text,
            ImagePath = imagePath,
            CreatedAt = DateTime.UtcNow,
            EventId = eventId,
            UserId = currentUserId
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Event", new { id = eventId, scroll = returnScroll });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int postId, int eventId, int returnScroll = 0)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (currentUserId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var post = await _context.Posts
            .Include(p => p.Event)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == postId && p.EventId == eventId);

        if (post == null)
        {
            return NotFound();
        }

        var isPostOwner = post.UserId == currentUserId;
        var isEventOwner = post.Event != null && post.Event.OwnerId == currentUserId;

        if (!isPostOwner && !isEventOwner)
        {
            return Forbid();
        }

        if (!string.IsNullOrWhiteSpace(post.ImagePath))
        {
            var imageFullPath = Path.Combine(
                _environment.WebRootPath,
                post.ImagePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
            );

            if (System.IO.File.Exists(imageFullPath))
            {
                System.IO.File.Delete(imageFullPath);
            }
        }

        _context.Likes.RemoveRange(post.Likes);
        _context.Comments.RemoveRange(post.Comments);
        _context.Posts.Remove(post);

        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Event", new { id = eventId, scroll = returnScroll });
    }
}