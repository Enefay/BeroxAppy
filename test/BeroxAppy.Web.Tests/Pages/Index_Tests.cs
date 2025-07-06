using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace BeroxAppy.Pages;

public class Index_Tests : BeroxAppyWebTestBase
{
    [Fact]
    public async Task Welcome_Page()
    {
        var response = await GetResponseAsStringAsync("/");
        response.ShouldNotBeNull();
    }
}
