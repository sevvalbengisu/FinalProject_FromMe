using System.Security.Claims;
using FinalProject_FromMe.Data;
using FinalProject_FromMe.Models;
using FinalProject_FromMe.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalProject_FromMe.Controllers;

[Authorize]
public class EventController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public EventController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var events = await _context.Events
            .Where(e => e.OwnerId == currentUserId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return View(events);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new EventCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (currentUserId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var newEvent = new Event
        {
            Title = model.Title,
            Description = model.Description,
            IsPublic = model.IsPublic,
            CreatedAt = DateTime.UtcNow,
            OwnerId = currentUserId
        };

        _context.Events.Add(newEvent);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = newEvent.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var eventItem = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventItem == null)
        {
            return NotFound();
        }

        if (eventItem.OwnerId != currentUserId)
        {
            return Forbid();
        }

        var viewModel = new EventEditViewModel
        {
            Id = eventItem.Id,
            Title = eventItem.Title,
            Description = eventItem.Description,
            IsPublic = eventItem.IsPublic
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EventEditViewModel model)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var eventItem = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == model.Id);

        if (eventItem == null)
        {
            return NotFound();
        }

        if (eventItem.OwnerId != currentUserId)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        eventItem.Title = model.Title;
        eventItem.Description = model.Description;
        eventItem.IsPublic = model.IsPublic;

        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = eventItem.Id });
    }

    public async Task<IActionResult> Details(int id, string sort = "newest")
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var eventItem = await _context.Events
            .Include(e => e.Owner)
            .Include(e => e.Posts)
                .ThenInclude(p => p.User)
            .Include(e => e.Posts)
                .ThenInclude(p => p.Likes)
            .Include(e => e.Posts)
                .ThenInclude(p => p.Comments)
                    .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventItem == null)
        {
            return NotFound();
        }

        var postViewModels = eventItem.Posts
            .Select(p => new PostViewModel
            {
                Id = p.Id,
                Text = p.Text,
                ImagePath = p.ImagePath,
                CreatedAt = p.CreatedAt,
                UserName = p.User != null ? p.User.UserName! : "Unknown User",
                LikeCount = p.Likes.Count,
                IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),
                CanCurrentUserDelete = p.UserId == currentUserId || eventItem.OwnerId == currentUserId,
                CanCurrentUserEdit = p.UserId == currentUserId,
                Comments = p.Comments
                    .OrderBy(c => c.CreatedAt)
                    .Select(c => new CommentViewModel
                    {
                        Id = c.Id,
                        Text = c.Text,
                        UserName = c.User != null ? c.User.UserName! : "Unknown User",
                        CreatedAt = c.CreatedAt
                    })
                    .ToList()
            })
            .ToList();

        postViewModels = sort switch
        {
            "oldest" => postViewModels.OrderBy(p => p.CreatedAt).ToList(),
            "mostLiked" => postViewModels.OrderByDescending(p => p.LikeCount).ThenByDescending(p => p.CreatedAt).ToList(),
            _ => postViewModels.OrderByDescending(p => p.CreatedAt).ToList()
        };

        var viewModel = new EventDetailViewModel
        {
            EventId = eventItem.Id,
            Title = eventItem.Title,
            Description = eventItem.Description,
            CreatedAt = eventItem.CreatedAt,
            OwnerName = eventItem.Owner != null ? eventItem.Owner.UserName! : "Unknown User",
            IsPublic = eventItem.IsPublic,
            IsCurrentUserEventOwner = eventItem.OwnerId == currentUserId,
            CurrentSort = sort,
            Posts = postViewModels,
            NewPost = new CreatePostViewModel
            {
                EventId = eventItem.Id
            }
        };

        return View(viewModel);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Explore(string? search, string? owner)
    {
        var query = _context.Events
            .Where(e => e.IsPublic)
            .Include(e => e.Owner)
            .Include(e => e.Posts)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(owner))
        {
            query = query.Where(e =>
                e.Owner != null &&
                e.Owner.UserName != null &&
                e.Owner.UserName.ToLower() == owner.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e =>
                e.Title.ToLower().Contains(search.ToLower()) ||
                (e.Owner != null &&
                 e.Owner.UserName != null &&
                 e.Owner.UserName.ToLower().Contains(search.ToLower())));
        }

        var events = await query
            .Select(e => new ExploreEventViewModel
            {
                EventId = e.Id,
                Title = e.Title,
                Description = e.Description,
                OwnerName = e.Owner != null ? e.Owner.UserName! : "Unknown User",
                CreatedAt = e.CreatedAt,
                LastActivityAt = e.Posts.Any() ? e.Posts.Max(p => p.CreatedAt) : e.CreatedAt,
                PostCount = e.Posts.Count
            })
            .OrderByDescending(e => e.LastActivityAt)
            .ToListAsync();

        var viewModel = new ExploreIndexViewModel
        {
            Search = search,
            Owner = owner,
            Events = events
        };

        return View(viewModel);
    }

    [AllowAnonymous]
    public IActionResult Join(int id)
    {
        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var eventItem = await _context.Events
            .Include(e => e.Posts)
                .ThenInclude(p => p.Likes)
            .Include(e => e.Posts)
                .ThenInclude(p => p.Comments)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventItem == null)
        {
            return NotFound();
        }

        if (eventItem.OwnerId != currentUserId)
        {
            return Forbid();
        }

        foreach (var post in eventItem.Posts)
        {
            DeleteImageFile(post.ImagePath);

            _context.Likes.RemoveRange(post.Likes);
            _context.Comments.RemoveRange(post.Comments);
        }

        _context.Posts.RemoveRange(eventItem.Posts);
        _context.Events.Remove(eventItem);

        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
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