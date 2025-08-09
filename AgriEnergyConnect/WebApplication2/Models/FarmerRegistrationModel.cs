using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using WebApplication2.Areas.Identity.Data;

namespace WebApplication2.Models
{
    public class FarmerRegistrationModel
    {
        /*
         * The code below was inspired by an online blog
         * Titled: User Registration with ASP.NET Core Identity
         * Uploaded by: CodeMaze
         * Available at: https://code-maze.com/user-registration-aspnet-core-identity/
         * Accessed 26 May 2024
         */
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(20, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
        [DataType(DataType.Text)]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Role")]
        public string Role { get; set; }
    }
}
