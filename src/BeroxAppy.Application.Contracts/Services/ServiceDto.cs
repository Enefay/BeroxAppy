using BeroxAppy.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace BeroxAppy.Services
{
    public class ServiceDto : FullAuditedEntityDto<Guid>
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public Gender TargetGender { get; set; }

        [Range(1, 1440)] // 1 dakika - 24 saat arası
        public int DurationMinutes { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;

        // Kategori ilişkisi
        [Display(Name = "Kategori")]
        [Required(ErrorMessage = "Lütfen bir kategori seçin.")]
        public Guid? CategoryId { get; set; }
        public string CategoryName { get; set; } // Include için

        // Display için hesaplanan alanlar
        public string? DurationDisplay { get; set; } // "1 saat 30 dakika"
        public string? TargetGenderDisplay { get; set; } // "Unisex", "Erkek", "Kadın"
    }
}
