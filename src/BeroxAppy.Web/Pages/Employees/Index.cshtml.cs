using System.Threading.Tasks;
using BeroxAppy.Employees;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace BeroxAppy.Web.Pages.Employees
{
    public class IndexModel : AbpPageModel
    {
        private readonly IEmployeeAppService _employeeAppService;

        public IndexModel(IEmployeeAppService employeeAppService)
        {
            _employeeAppService = employeeAppService;
        }

        public async Task OnGetAsync()
        {

        }
    }
}