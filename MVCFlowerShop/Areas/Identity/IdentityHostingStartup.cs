using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MVCFlowerShop.Areas.Identity.Data;
using MVCFlowerShop.Data;

[assembly: HostingStartup(typeof(MVCFlowerShop.Areas.Identity.IdentityHostingStartup))]
namespace MVCFlowerShop.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
                services.AddDbContext<MVCFlowerShopContext>(options =>
                    options.UseSqlServer(
                        context.Configuration.GetConnectionString("MVCFlowerShopContextConnection")));

                services.AddDefaultIdentity<MVCFlowerShopUser>(options => options.SignIn.RequireConfirmedAccount = true)
                    .AddEntityFrameworkStores<MVCFlowerShopContext>();
            });
        }
    }
}