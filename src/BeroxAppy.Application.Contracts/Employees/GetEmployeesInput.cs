using BeroxAppy.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace BeroxAppy.Employees
{
    public class GetEmployeesInput : PagedAndSortedResultRequestDto
    {
        public string? Filter { get; set; } // Ad, soyad, telefon, email'de arama
        public EmployeeType? EmployeeType { get; set; }
        public Gender? ServiceGender { get; set; }
        public bool? IsActive { get; set; }
        public bool? CanTakeOnlineReservation { get; set; }
        public bool? HasUser { get; set; } // Kullanıcısı olan/olmayan
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
    }
}
