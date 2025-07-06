using BeroxAppy.Samples;
using Xunit;

namespace BeroxAppy.EntityFrameworkCore.Domains;

[Collection(BeroxAppyTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<BeroxAppyEntityFrameworkCoreTestModule>
{

}
