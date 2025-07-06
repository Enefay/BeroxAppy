using BeroxAppy.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace BeroxAppy.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(BeroxAppyEntityFrameworkCoreModule),
    typeof(BeroxAppyApplicationContractsModule)
    )]
public class BeroxAppyDbMigratorModule : AbpModule
{
}
