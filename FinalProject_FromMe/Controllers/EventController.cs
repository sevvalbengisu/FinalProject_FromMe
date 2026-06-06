using FinalProject_FromMe.Data;
using FinalProject_FromMe.Models;
using FinalProject_FromMe.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalProject_FromMe.Controllers;

[Authorize]
public class EventController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public EventController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var currentUserId = _userManager.GetUserId(User);

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

        var currentUserId = _userManager.GetUserId(User);

        if (currentUserId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var newEvent = new Event
        {
            Title = model.Title,
            Description = model.Description,
            CreatedAt = DateTime.UtcNow,
            IsPublic = model.IsPublic,
            OwnerId = currentUserId
        };

        _context.Events.Add(newEvent);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = newEvent.Id });
    }

    public async Task<IActionResult> Details(int id, string sort = "newest")
    {
        var currentUserId = _userManager.GetUserId(User);

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

        var postsQuery = eventItem.Posts.AsEnumerable();

        if (sort == "oldest")
        {
            postsQuery = postsQuery.OrderBy(p => p.CreatedAt);
        }
        else if (sort == "mostLiked")
        {
            postsQuery = postsQuery
                .OrderByDescending(p => p.Likes.Count)
                .ThenByDescending(p => p.CreatedAt);
        }
        else
        {
            sort = "newest";
            postsQuery = postsQuery.OrderByDescending(p => p.CreatedAt);
        }

        var viewModel = new EventDetailViewModel
        {
            EventId = eventItem.Id,
            Title = eventItem.Title,
            Description = eventItem.Description,
            CreatedAt = eventItem.CreatedAt,
            OwnerName = eventItem.Owner?.UserName ?? "Unknown User",
            IsPublic = eventItem.IsPublic,
            IsCurrentUserEventOwner = eventItem.OwnerId == currentUserId,
            CurrentSort = sort,
            NewPost = new CreatePostViewModel
            {
                EventId = eventItem.Id
            },
            Posts = postsQuery
                .Select(p => new PostViewModel
                {
                    Id = p.Id,
                    Text = p.Text,
                    ImagePath = p.ImagePath,
                    CreatedAt = p.CreatedAt,
                    UserName = p.User?.UserName ?? "Unknown User",
                    LikeCount = p.Likes.Count,
                    IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),
                    CanCurrentUserDelete = p.UserId == currentUserId || eventItem.OwnerId == currentUserId,
                    Comments = p.Comments
                        .OrderBy(c => c.CreatedAt)
                        .Select(c => new CommentViewModel
                        {
                            Id = c.Id,
                            Text = c.Text,
                            UserName = c.User?.UserName ?? "Unknown User",
                            CreatedAt = c.CreatedAt
                        })
                        .ToList()
                })
                .ToList()
        };

        return View(viewModel);
    }

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
                PostCount = e.Posts.Count,
                LastActivityAt = e.Posts.Any()
                    ? e.Posts.Max(p => p.CreatedAt)
                    : e.CreatedAt
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

    public IActionResult Join(int id)
    {
        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = _userManager.GetUserId(User);

        if (currentUserId == null)
        {
            return Unauthorized();
        }

        var eventItem = await _context.Events
            .Include(e => e.Posts)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventItem == null)
        {
            return NotFound();
        }

        if (eventItem.OwnerId != currentUserId)
        {
            return Forbid();
        }

        _context.Events.Remove(eventItem);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }
}