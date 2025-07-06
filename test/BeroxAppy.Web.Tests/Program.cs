using Microsoft.AspNetCore.Builder;
using BeroxAppy;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();

builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("BeroxAppy.Web.csproj");
await builder.RunAbpModuleAsync<BeroxAppyWebTestModule>(applicationName: "BeroxAppy.Web" );

public partial class Program
{
}
