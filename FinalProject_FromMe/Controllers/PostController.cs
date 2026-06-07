using System.Security.Claims;
using FinalProject_FromMe.Data;
using FinalProject_FromMe.Models;
using FinalProject_FromMe.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace FinalProject_FromMe.Controllers;

[Authorize]
public class PostController : Controller
{
    private const long MaxImageSize = 5 * 1024 * 1024;
    private const int MaxImageWidth = 1024;

    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

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
            TempData["PostError"] = "Please write a text or upload an image.";
            return RedirectToAction("Details", "Event", new { id = eventId, scroll = returnScroll });
        }

        string? imagePath = null;

        if (image != null && image.Length > 0)
        {
            var validationError = ValidateImage(image);

            if (validationError != null)
            {
                TempData["PostError"] = validationError;
                return RedirectToAction("Details", "Event", new { id = eventId, scroll = returnScroll });
            }

            try
            {
                imagePath = await SaveImageAsync(image);
            }
            catch
            {
                TempData["PostError"] = "Uploaded file is not a valid image.";
                return RedirectToAction("Details", "Event", new { id = eventId, scroll = returnScroll });
            }
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

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
        {
            return NotFound();
        }

        if (post.UserId != currentUserId)
        {
            return Forbid();
        }

        var viewModel = new PostEditViewModel
        {
            Id = post.Id,
            EventId = post.EventId,
            Text = post.Text,
            ExistingImagePath = post.ImagePath
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PostEditViewModel model)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == model.Id);

        if (post == null)
        {
            return NotFound();
        }

        if (post.UserId != currentUserId)
        {
            return Forbid();
        }

        model.EventId = post.EventId;
        model.ExistingImagePath = post.ImagePath;

        if (string.IsNullOrWhiteSpace(model.Text) &&
            string.IsNullOrWhiteSpace(post.ImagePath) &&
            model.Image == null)
        {
            ModelState.AddModelError("", "Post must have text or an image.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.Image != null && model.Image.Length > 0)
        {
            var validationError = ValidateImage(model.Image);

            if (validationError != null)
            {
                ModelState.AddModelError("Image", validationError);
                return View(model);
            }

            try
            {
                var newImagePath = await SaveImageAsync(model.Image);

                DeleteImageFile(post.ImagePath);

                post.ImagePath = newImagePath;
            }
            catch
            {
                ModelState.AddModelError("Image", "Uploaded file is not a valid image.");
                return View(model);
            }
        }

        post.Text = model.Text;

        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Event", new { id = post.EventId });
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

        DeleteImageFile(post.ImagePath);

        _context.Likes.RemoveRange(post.Likes);
        _context.Comments.RemoveRange(post.Comments);
        _context.Posts.Remove(post);

        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Event", new { id = eventId, scroll = returnScroll });
    }

    private string? ValidateImage(IFormFile image)
    {
        if (image.Length > MaxImageSize)
        {
            return "Image size must be less than 5 MB.";
        }

        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

        if (!_allowedExtensions.Contains(extension))
        {
            return "Only .jpg, .jpeg, .png and .webp images are allowed.";
        }

        if (!image.ContentType.StartsWith("image/"))
        {
            return "Uploaded file must be an image.";
        }

        return null;
    }

    private async Task<string> SaveImageAsync(IFormFile image)
    {
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "posts");

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(uploadsFolder, fileName);

        await using var inputStream = image.OpenReadStream();
        using var loadedImage = await Image.LoadAsync(inputStream);

        if (loadedImage.Width > MaxImageWidth)
        {
            var ratio = (double)MaxImageWidth / loadedImage.Width;
            var newHeight = (int)Math.Round(loadedImage.Height * ratio);

            loadedImage.Mutate(x => x.Resize(MaxImageWidth, newHeight));
        }

        await loadedImage.SaveAsync(fullPath);

        return $"/uploads/posts/{fileName}";
    }

    private void DeleteImageFile(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return;
        }

        var imageFullPath = Path.Combine(
            _environment.WebRootPath,
            imagePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
        );

        if (System.IO.File.Exists(imageFullPath))
        {
            System.IO.File.Delete(imageFullPath);
        }
    }
}