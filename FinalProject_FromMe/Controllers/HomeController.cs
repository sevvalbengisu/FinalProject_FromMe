using System.Diagnostics;
using System.Security.Claims;
using FinalProject_FromMe.Data;
using FinalProject_FromMe.Models;
using FinalProject_FromMe.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalProject_FromMe.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new HomeIndexViewModel();

        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            viewModel.RecentPosts = await _context.Posts
                .Where(p => p.UserId == currentUserId)
                .Include(p => p.User)
                .Include(p => p.Event)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .OrderByDescending(p => p.CreatedAt)
                .Take(12)
                .Select(p => new HomePostViewModel
                {
                    PostId = p.Id,
                    Text = p.Text,
                    ImagePath = p.ImagePath,
                    CreatedAt = p.CreatedAt,
                    UserName = p.User != null ? p.User.UserName! : "Unknown User",
                    EventId = p.EventId,
                    EventTitle = p.Event != null ? p.Event.Title : "Unknown Event",
                    EventIsPublic = p.Event != null && p.Event.IsPublic,
                    LikeCount = p.Likes.Count,
                    CommentCount = p.Comments.Count
                })
                .ToListAsync();
        }

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}