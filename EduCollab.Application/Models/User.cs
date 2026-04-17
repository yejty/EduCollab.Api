using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduCollab.Application.Models
{
    public class User 
    {
        [MaxLength(100), Required]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100), Required]
        public string LastName { get; set; } = string.Empty;

        [EmailAddress, Required]
        public string? Email { get; set; }

        public string FullName => $"{this.FirstName} {this.LastName}";

    }
}
