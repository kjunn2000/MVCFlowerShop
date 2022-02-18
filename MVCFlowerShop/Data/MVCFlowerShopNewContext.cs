using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MVCFlowerShop.Models;

namespace MVCFlowerShop.Data
{
    public class MVCFlowerShopNewContext : DbContext
    {
        public MVCFlowerShopNewContext (DbContextOptions<MVCFlowerShopNewContext> options)
            : base(options)
        {
        }

        public DbSet<MVCFlowerShop.Models.Flower> Flower { get; set; }

        public DbSet<MVCFlowerShop.Models.Payment> Payment { get; set; }
    }
}
