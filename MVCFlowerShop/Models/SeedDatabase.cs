using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MVCFlowerShop.Data;
using System;
using System.Linq;

namespace MVCFlowerShop.Models
{
    public class SeedDatabase
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new MVCFlowerShopNewContext(
                serviceProvider.GetRequiredService<DbContextOptions<MVCFlowerShopNewContext>>()))
            {
                if(context.Flower.Any() && context.Payment.Any())
                {
                    return;
                }

                if (!context.Flower.Any())
                {
                    context.Flower.AddRange(
                    new Flower
                    {
                        FlowerName = "Yellow Sunflower",
                        FlowerProducedDate = DateTime.Parse("2020-01-30"),
                        Price = 3.45M,
                        Type = "Sunflower",
                        Rating = "3.3"
                    },
                    new Flower
                    {
                        FlowerName = "White Rose",
                        FlowerProducedDate = DateTime.Parse("2020-01-26"),
                        Price = 6.45M,
                        Type = "Rose",
                        Rating = "3.4"
                    }
                );
                }

                if (!context.Payment.Any())
                {
                    context.Payment.AddRange(
                        new Payment
                        {
                            CustomerName = "Test Jun",
                            PaymentDaten = DateTime.Parse("2020-01-26"),
                            PaymentAmount = 15M
                        }
                    );
                }
                
                context.SaveChanges();

            }
        }
    }
}
