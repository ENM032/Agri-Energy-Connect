using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using WebApplication2.Models;

namespace WebApplication2.Areas.Identity.Data;

// Add profile data for application users by adding properties to the WebApplication2User class
public class WebApplication2User : IdentityUser
{
    public string? Displayname { get; set; }
    // This code was taken from a website 
    // Availble at: https://learn.microsoft.com/en-us/ef/core/modeling/relationships
    // Accessed 26 May 2024
    public ICollection<Product> Products { get; set; }
}

