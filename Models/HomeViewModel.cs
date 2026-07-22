using NewsApp.Models;
using System.Collections.Generic;

namespace NewsApp.ViewModels
{
    public class HomeViewModel
    {
        public List<Category> Categories { get; set; } = new();

        public List<News> News { get; set; } = new();

        public string BreakingNews { get; set; } = string.Empty;

        public List<News> FeaturedNews { get; set; } = new();

        public List<News> LatestNews { get; set; } = new();

        public List<News> TrendingNews { get; set; } = new();

        public int CurrentPage { get; set; }

        public int PageSize { get; set; }

        public int TotalItems { get; set; }

        public int TotalPages { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;

        public bool HasNextPage => CurrentPage < TotalPages;
    }
}