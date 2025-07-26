using BeroxAppy.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace BeroxAppy.Employees
{
    public class EmployeeUpdateDto : FullAuditedEntityDto<Guid>
    {
        [Display(Name = "Ad")]
        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        [MaxLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir.")]
        public string FirstName { get; set; }

        [Display(Name = "Soyad")]
        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        [MaxLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir.")]
        public string LastName { get; set; }

        [Display(Name = "Telefon")]
        [Required(ErrorMessage = "Telefon alanı zorunludur.")]
        [MaxLength(20, ErrorMessage = "Telefon en fazla 20 karakter olabilir.")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        public string Phone { get; set; }

        [Display(Name = "E‑posta")]
        [MaxLength(100, ErrorMessage = "E‑posta en fazla 100 karakter olabilir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e‑posta adresi giriniz.")]
        public string Email { get; set; }

        [Display(Name = "Kullanıcı Id")]
        public Guid? UserId { get; set; } // ABP User ile ilişki

        [Display(Name = "Çalışan Tipi")]
        public EmployeeType EmployeeType { get; set; }

        [Display(Name = "Hizmet Cinsiyeti")]
        public Gender ServiceGender { get; set; }

        [Display(Name = "Takvim Rengi")]
        [MaxLength(7, ErrorMessage = "Renk kodu en fazla 7 karakter olabilir.")]
        public string CalendarColor { get; set; } = "#3498db";

        [Display(Name = "Çevrimiçi Rezervasyon Alabilir")]
        public bool CanTakeOnlineReservation { get; set; } = true;

        [Display(Name = "Sabit Ücret")]
        [Range(0, double.MaxValue, ErrorMessage = "Sabit ücret negatif olamaz.")]
        public decimal FixedSalary { get; set; }

        [Display(Name = "Hizmet Komisyon Oranı (%)")]
        [Range(0, 100, ErrorMessage = "Hizmet komisyon oranı 0 ile 100 arasında olmalıdır.")]
        public decimal ServiceCommissionRate { get; set; }

        [Display(Name = "Ürün Komisyon Oranı (%)")]
        [Range(0, 100, ErrorMessage = "Ürün komisyon oranı 0 ile 100 arasında olmalıdır.")]
        public decimal ProductCommissionRate { get; set; }

        [Display(Name = "Paket Komisyon Oranı (%)")]
        [Range(0, 100, ErrorMessage = "Paket komisyon oranı 0 ile 100 arasında olmalıdır.")]
        public decimal PackageCommissionRate { get; set; }

        [Display(Name = "Aktif Mi?")]
        public bool IsActive { get; set; } = true;

        // Display için hesaplanan alanlar
        [Display(Name = "Tam Ad")]
        public string FullName { get; set; } // FirstName + LastName

        [Display(Name = "Çalışan Tipi Görünümü")]
        public string EmployeeTypeDisplay { get; set; } // "Personel", "Sekreter" vb.

        [Display(Name = "Hizmet Cinsiyeti Görünümü")]
        public string ServiceGenderDisplay { get; set; } // "Erkek", "Kadın", "Unisex"

        [Display(Name = "Kullanıcı Oluşturuldu Mu?")]
        public bool HasUser { get; set; } // Kullanıcısı var mı?

        [Display(Name = "Kullanıcı Durumu")]
        public string UserStatus { get; set; } // "Aktif", "Pasif", "Kullanıcı Yok"
    }
}
