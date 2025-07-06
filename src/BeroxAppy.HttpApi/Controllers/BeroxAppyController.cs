using BeroxAppy.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace BeroxAppy.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class BeroxAppyController : AbpControllerBase
{
    protected BeroxAppyController()
    {
        LocalizationResource = typeof(BeroxAppyResource);
    }
}
