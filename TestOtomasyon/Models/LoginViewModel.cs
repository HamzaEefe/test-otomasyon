using System.ComponentModel.DataAnnotations;

namespace TestOtomasyon.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Validation.UserNameRequired")]
        [Display(Name = "Field.UserName")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation.PasswordRequired")]
        [DataType(DataType.Password)]
        [Display(Name = "Field.Password")]
        public string Password { get; set; } = string.Empty;
    }
}
