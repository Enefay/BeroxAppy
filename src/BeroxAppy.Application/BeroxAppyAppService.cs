using System;
using System.Collections.Generic;
using System.Text;
using BeroxAppy.Localization;
using Volo.Abp.Application.Services;

namespace BeroxAppy;

/* Inherit your application services from this class.
 */
public abstract class BeroxAppyAppService : ApplicationService
{
    protected BeroxAppyAppService()
    {
        LocalizationResource = typeof(BeroxAppyResource);
    }
}
