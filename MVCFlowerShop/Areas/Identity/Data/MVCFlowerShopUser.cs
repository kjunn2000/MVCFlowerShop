using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace MVCFlowerShop.Areas.Identity.Data
{
    // Add profile data for application users by adding properties to the MVCFlowerShopUser class
    public class MVCFlowerShopUser : IdentityUser
    {
        [PersonalData]
        public string FullName { get; set; }

        [PersonalData]
        public DateTime DOB { get; set; }

        [PersonalData]
        public int Age { get; set; }

        [PersonalData]
        public string Address { get; set; }
    }
}
