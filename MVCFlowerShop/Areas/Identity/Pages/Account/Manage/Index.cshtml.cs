using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MVCFlowerShop.Areas.Identity.Data;

namespace MVCFlowerShop.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : PageModel
    {
        private readonly UserManager<MVCFlowerShopUser> _userManager;
        private readonly SignInManager<MVCFlowerShopUser> _signInManager;

        public IndexModel(
            UserManager<MVCFlowerShopUser> userManager,
            SignInManager<MVCFlowerShopUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }

            [Required(ErrorMessage ="Must key in name before submit the form")]
            [Display(Name = "User Full Name")]
            [StringLength(256, ErrorMessage ="Name must be in between 256 characteres", MinimumLength =10)]
            public string UserFullName { get; set; }

            [Required]
            [Display(Name = "User DOB")]
            [DataType(DataType.Date)]
            public DateTime DOB { get; set; }

            [Required]
            [Display(Name = "User Age")]
            [Range(18, 65, ErrorMessage ="We only allow 18 to 65 years old person to become our member")]
            public int Age { get; set; }

            [Required]
            [Display(Name = "User Address")]
            public string Address { get; set; }
        }

        private async Task LoadAsync(MVCFlowerShopUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                UserFullName = user.FullName,
                Address = user.Address,
                DOB = user.DOB,
                Age = user.Age
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            if (Input.UserFullName != user.FullName)
            {
                user.FullName = Input.UserFullName;
            }
            if (Input.Age != user.Age)
            {
                user.Age = Input.Age;
            }
            if (Input.DOB != user.DOB)
            {
                user.DOB = Input.DOB;
            }
            if (Input.Address != user.Address)
            {
                user.Address = Input.Address;
            }
            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
