using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using BeroxAppy.Services;
using BeroxAppy.Enums;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace BeroxAppy.Web.Pages.Services.Services
{
    public class CreateEditModalModel : AbpPageModel
    {
        [HiddenInput]
        [BindProperty(SupportsGet = true)]
        public Guid Id { get; set; }

        [BindProperty]
        public ServiceDto Service { get; set; }

        public List<SelectListItem> Categories { get; set; } = new();

        private readonly IServiceAppService _serviceAppService;
        private readonly IServiceCategoryAppService _serviceCategoryAppService;

        public CreateEditModalModel(
            IServiceAppService serviceAppService,
            IServiceCategoryAppService serviceCategoryAppService)
        {
            _serviceAppService = serviceAppService;
            _serviceCategoryAppService = serviceCategoryAppService;
        }

        public async Task OnGetAsync()
        {
            await LoadCategoriesAsync();

            if (Id != Guid.Empty)
            {
                var serviceDto = await _serviceAppService.GetAsync(Id);
                Service = new ServiceDto
                {
                    Title = serviceDto.Title,
                    Description = serviceDto.Description,
                    TargetGender = serviceDto.TargetGender,
                    DurationMinutes = serviceDto.DurationMinutes,
                    Price = serviceDto.Price,
                    IsActive = serviceDto.IsActive,
                    CategoryId = serviceDto.CategoryId
                };
            }
            else
            {
                Service = new ServiceDto
                {
                    TargetGender = Gender.Unisex,
                    DurationMinutes = 30,
                    Price = 0,
                    IsActive = true
                };
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (Id != Guid.Empty)
                {
                    await _serviceAppService.UpdateAsync(Id, Service);
                }
                else
                {
                    await _serviceAppService.CreateAsync(Service);
                }

                return NoContent();
            }
            catch (Volo.Abp.BusinessException ex)
            {
                ModelState.AddModelError("", ex.Message);
                await LoadCategoriesAsync();
                return Page();
            }
        }

        private async Task LoadCategoriesAsync()
        {
            var categories = await _serviceCategoryAppService.GetActiveListAsync();
            Categories = categories.Items
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToList();
        }
    }
}