// Pages/Employees/CreateEditModal.cshtml.cs
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BeroxAppy.Employees;
using BeroxAppy.Enums;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace BeroxAppy.Web.Pages.Employees
{
    public class CreateEditModalModel : AbpPageModel
    {
        [HiddenInput]
        [BindProperty(SupportsGet = true)]
        public Guid Id { get; set; }

        [BindProperty]
        public EmployeeDto Employee { get; set; }

        private readonly IEmployeeAppService _employeeAppService;

        public CreateEditModalModel(IEmployeeAppService employeeAppService)
        {
            _employeeAppService = employeeAppService;
        }

        public async Task OnGetAsync()
        {
            if (Id != Guid.Empty)
            {
                var employeeDto = await _employeeAppService.GetAsync(Id);
                Employee = new EmployeeDto
                {
                    FirstName = employeeDto.FirstName,
                    LastName = employeeDto.LastName,
                    Phone = employeeDto.Phone,
                    Email = employeeDto.Email,
                    EmployeeType = employeeDto.EmployeeType,
                    ServiceGender = employeeDto.ServiceGender,
                    CalendarColor = employeeDto.CalendarColor,
                    CanTakeOnlineReservation = employeeDto.CanTakeOnlineReservation,
                    FixedSalary = employeeDto.FixedSalary,
                    ServiceCommissionRate = employeeDto.ServiceCommissionRate,
                    ProductCommissionRate = employeeDto.ProductCommissionRate,
                    PackageCommissionRate = employeeDto.PackageCommissionRate,
                    IsActive = employeeDto.IsActive
                };
            }
            else
            {
                Employee = new EmployeeDto
                {
                    EmployeeType = EmployeeType.Staff,
                    ServiceGender = Gender.Unisex,
                    CalendarColor = "#3498db",
                    CanTakeOnlineReservation = true,
                    FixedSalary = 0,
                    ServiceCommissionRate = 0,
                    ProductCommissionRate = 0,
                    PackageCommissionRate = 0,
                    IsActive = true
                };
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Telefon numaras� temizleme
                if (!string.IsNullOrWhiteSpace(Employee.Phone))
                {
                    Employee.Phone = Employee.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
                }

                // Renk kontrol�
                if (string.IsNullOrWhiteSpace(Employee.CalendarColor))
                {
                    Employee.CalendarColor = "#3498db";
                }

                // Create/Update i�lemi
                if (Id != Guid.Empty)
                {
                    await _employeeAppService.UpdateAsync(Id, Employee);
                }
                else
                {
                    // Create i�leminde UserName/Password kontrol�
                    if (!string.IsNullOrWhiteSpace(Employee.UserName))
                    {
                        if (string.IsNullOrWhiteSpace(Employee.Password))
                        {
                            ModelState.AddModelError("Employee.Password", "Kullan�c� ad� girildi�inde �ifre zorunludur.");
                            return Page();
                        }

                        if (Employee.Password.Length < 6)
                        {
                            ModelState.AddModelError("Employee.Password", "�ifre en az 6 karakter olmal�d�r.");
                            return Page();
                        }
                    }

                    await _employeeAppService.CreateAsync(Employee);
                }

                return NoContent();
            }
            catch (Volo.Abp.BusinessException ex)
            {
                // Email/telefon duplicate hatas�
                if (ex.Message.Contains("email"))
                {
                    ModelState.AddModelError("Employee.Email", ex.Message);
                    return Page();
                }
                if (ex.Message.Contains("telefon"))
                {
                    ModelState.AddModelError("Employee.Phone", ex.Message);
                    return Page();
                }

                ModelState.AddModelError("", ex.Message);
                return Page();
            }
        }
    }
}