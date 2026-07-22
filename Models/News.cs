using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewsApp.Models
{
    public class News
    {
        public int Id { get; set; }

        // =====================================================
        // ENGLISH CONTENT
        // =====================================================

        [Required]
        [Display(Name = "Title (English)")]
        public string TitleEnglish { get; set; }

        [Required]
        [Display(Name = "Description (English)")]
        public string DescriptionEnglish { get; set; }

        // =====================================================
        // BANGLA CONTENT
        // =====================================================

        [Required]
        [Display(Name = "Title (Bangla)")]
        public string TitleBangla { get; set; }

        [Required]
        [Display(Name = "Description (Bangla)")]
        public string DescriptionBangla { get; set; }

        // =====================================================
        // MEDIA
        // =====================================================

        public string? Image { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        // =====================================================
        // VIDEO
        // =====================================================

        public string? VideoPath { get; set; }

        public string? VideoUrl { get; set; }

        [NotMapped]
        public IFormFile? VideoFile { get; set; }

        // =====================================================
        // DATE
        // =====================================================

        public DateTime Date { get; set; } = DateTime.Now;

        // =====================================================
        // FLAGS
        // =====================================================

        public bool IsBreaking { get; set; }

        public bool IsFeatured { get; set; }

        // =====================================================
        // CATEGORY
        // =====================================================

        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        public Category? Category { get; set; }
    }
}