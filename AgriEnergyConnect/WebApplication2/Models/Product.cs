using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebApplication2.Areas.Identity.Data;

namespace WebApplication2.Models
{
    public class Product
    {
        /* This code was inspired by an online blog 
         * Titled: Introduction to relationships
         * Uploaded by: Microsoft
         * Availble at: https://learn.microsoft.com/en-us/ef/core/modeling/relationships
         * Accessed 26 May 2024
        */
        public int Id { get; set; }

        public required string Name { get; set; }

        public required string Category { get; set; }

        [DataType(DataType.Date)]
        public DateTime ProductDate { get; set; }

        public string UserId { get; set; }

        [ValidateNever]
        public WebApplication2User User { get; set; }
    }
}
