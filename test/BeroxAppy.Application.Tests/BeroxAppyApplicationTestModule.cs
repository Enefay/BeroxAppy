using Volo.Abp.Modularity;

namespace BeroxAppy;

[DependsOn(
    typeof(BeroxAppyApplicationModule),
    typeof(BeroxAppyDomainTestModule)
)]
public class BeroxAppyApplicationTestModule : AbpModule
{

}
