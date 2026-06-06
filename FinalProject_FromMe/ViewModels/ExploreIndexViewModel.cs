namespace FinalProject_FromMe.ViewModels;

public class ExploreIndexViewModel
{
    public string? Search { get; set; }

    public string? Owner { get; set; }

    public List<ExploreEventViewModel> Events { get; set; } = new List<ExploreEventViewModel>();
}