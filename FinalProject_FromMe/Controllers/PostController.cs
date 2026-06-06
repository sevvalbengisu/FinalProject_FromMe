using FinalProject_FromMe.Data;
using FinalProject_FromMe.Models;
using FinalProject_FromMe.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalProject_FromMe.Controllers;

[Authorize]
public class PostController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public PostController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePostViewModel model)
    {
        var currentUserId = _userManager.GetUserId(User);

        if (currentUserId == null)
        {
            return Unauthorized();
        }

        var eventExists = await _context.Events.AnyAsync(e => e.Id == model.EventId);

        if (!eventExists)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(model.Text) && model.Image == null)
        {
            return Redirect(Url.Action("Details", "Event", new { id = model.EventId }) + "#share-form");
        }

        string? imagePath = null;

        if (model.Image != null && model.Image.Length > 0)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "posts");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileExtension = Path.GetExtension(model.Image.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.Image.CopyToAsync(fileStream);
            }

            imagePath = $"/uploads/posts/{uniqueFileName}";
        }

        var post = new Post
        {
            Text = model.Text,
            ImagePath = imagePath,
            EventId = model.EventId,
            UserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        return Redirect(Url.Action("Details", "Event", new { id = model.EventId }) + $"#post-{post.Id}");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int postId, int eventId)
    {
        var currentUserId = _userManager.GetUserId(User);

        if (currentUserId == null)
        {
            return Unauthorized();
        }

        var post = await _context.Posts
            .Include(p => p.Event)
            .FirstOrDefaultAsync(p => p.Id == postId);

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
            var relativePath = post.ImagePath.TrimStart('/');
            var fullPath = Path.Combine(_environment.WebRootPath, relativePath);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Event", new { id = eventId });
    }
}