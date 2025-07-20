using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeroxAppy.Employees;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace BeroxAppy.Web.Pages.Employees
{
    public class WorkingHoursModalModel : BeroxAppyPageModel
    {
        private readonly IEmployeeAppService _employeeAppService;

        public WorkingHoursModalModel(IEmployeeAppService employeeAppService)
        {
            _employeeAppService = employeeAppService;
        }

        [BindProperty(SupportsGet = true)]
        public Guid EmployeeId { get; set; }

        public string EmployeeName { get; set; }
        public List<EmployeeWorkingHoursDto> WorkingHours { get; set; } = new();

        public Dictionary<DayOfWeek, string> DaysOfWeek { get; } = new()
        {
            { DayOfWeek.Monday, "Pazartesi" },
            { DayOfWeek.Tuesday, "Salý" },
            { DayOfWeek.Wednesday, "Çarþamba" },
            { DayOfWeek.Thursday, "Perþembe" },
            { DayOfWeek.Friday, "Cuma" },
            { DayOfWeek.Saturday, "Cumartesi" },
            { DayOfWeek.Sunday, "Pazar" },
        };

        public async Task<IActionResult> OnGetAsync()
        {
            var employee = await _employeeAppService.GetAsync(EmployeeId);
            EmployeeName = employee.FullName;

            var whResult = await _employeeAppService.GetWorkingHoursAsync(EmployeeId);
            WorkingHours = whResult.Items.ToList();

            return Page();
        }
    }
}
