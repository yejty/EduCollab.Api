using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduCollab.Contracts.Requests
{

    public class ChangePasswordRequest
    {
        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string NewPassword { get; set; } = string.Empty;
        
    }
}
