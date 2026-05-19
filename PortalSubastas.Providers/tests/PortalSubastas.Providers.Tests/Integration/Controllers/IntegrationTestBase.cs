using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortalSubastas.Providers.API;
using Microsoft.AspNetCore.Hosting;

namespace PortalSubastas.Providers.Tests.Integration.Controllers;

public class IntegrationTestBase : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContext));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<DbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
            });
        });

        builder.UseEnvironment("Testing");
    }
}
