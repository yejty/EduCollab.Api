using System.ComponentModel.DataAnnotations;

namespace EduCollab.Contracts.Requests.Users
{
    public class InviteUserRequest
    {
        [Required]
        [EmailAddress]
        [RegularExpression(@"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", ErrorMessage = "Invalid email address"), DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;

    }
}
