using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MVCFlowerShop.Models;
using Microsoft.Extensions.DependencyInjection;
using MVCFlowerShop.Data;
using Microsoft.EntityFrameworkCore;

namespace MVCFlowerShop
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            using (var scope = host.Services.CreateScope())
            {
                var serviceProvider = scope.ServiceProvider;
                try
                {
                    var context = serviceProvider.GetRequiredService<MVCFlowerShopNewContext>();
                    context.Database.Migrate();
                    SeedDatabase.Initialize(serviceProvider);
                }
                catch(Exception ex)
                {
                    var logger =
                        serviceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An occurred seeding the DB.");
                };
            }
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
