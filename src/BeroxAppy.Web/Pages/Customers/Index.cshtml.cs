using System.Threading.Tasks;
using BeroxAppy.Customers;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace BeroxAppy.Web.Pages.Customers
{
    public class IndexModel : AbpPageModel
    {
        private readonly ICustomerAppService _customerAppService;

        public IndexModel(ICustomerAppService customerAppService)
        {
            _customerAppService = customerAppService;
        }

        public async Task OnGetAsync()
        {

        }
    }
}