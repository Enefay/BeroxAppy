using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using BeroxAppy.Employees;
using BeroxAppy.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace BeroxAppy.Web.Pages.Employees
{
    public class ServiceAssignmentModalModel : BeroxAppyPageModel
    {
        private readonly IEmployeeAppService _employeeAppService;
        private readonly IServiceAppService _serviceAppService;
        private readonly IServiceCategoryAppService _serviceCategoryAppService;

        public ServiceAssignmentModalModel(
            IEmployeeAppService employeeAppService,
            IServiceAppService serviceAppService,
            IServiceCategoryAppService serviceCategoryAppService)
        {
            _employeeAppService = employeeAppService;
            _serviceAppService = serviceAppService;
            _serviceCategoryAppService = serviceCategoryAppService;
        }

        [BindProperty(SupportsGet = true)]
        public Guid EmployeeId { get; set; }

        public string EmployeeName { get; set; }
        public List<EmployeeServiceAssignmentDto> AssignedServices { get; set; } = new();
        public List<ServiceDto> AvailableServices { get; set; } = new();
        public List<ServiceCategoryDto> ServiceCategories { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                if (EmployeeId == Guid.Empty)
                {
                    return BadRequest("Employee ID is required");
                }

                // Çalýþan bilgilerini al
                var employee = await _employeeAppService.GetAsync(EmployeeId);
                EmployeeName = employee.FullName;

                // Atanmýþ hizmetleri al
                var assignedServicesResult = await _employeeAppService.GetEmployeeServicesAsync(EmployeeId);
                AssignedServices = assignedServicesResult.Items.ToList();

                // Tüm aktif hizmetleri al
                var allServicesResult = await _serviceAppService.GetActiveListAsync();
                var allServices = allServicesResult.Items.ToList();

                // Atanmýþ hizmet ID'lerini al
                var assignedServiceIds = AssignedServices.Select(x => x.ServiceId).ToHashSet();

                // Atanmamýþ hizmetleri filtrele
                AvailableServices = allServices.Where(x => !assignedServiceIds.Contains(x.Id)).ToList();

                // Kategorileri al
                var categoriesResult = await _serviceCategoryAppService.GetActiveListAsync();
                ServiceCategories = categoriesResult.Items.ToList();

                return Page();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading service assignment modal for employee {EmployeeId}", EmployeeId);
                return BadRequest($"Hata: {ex.Message}");
            }
        }
    }
}