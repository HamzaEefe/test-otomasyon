using System.ComponentModel.DataAnnotations;

namespace TestOtomasyon.Models
{
    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "Validation.FirstNameRequired")]
        [Display(Name = "Field.FirstName")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation.LastNameRequired")]
        [Display(Name = "Field.LastName")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation.UserNameRequired")]
        [Display(Name = "Field.UserName")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation.PasswordRequired")]
        [MinLength(6, ErrorMessage = "Validation.PasswordMinLength")]
        [DataType(DataType.Password)]
        [Display(Name = "Field.Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Field.Department")]
        public Guid? DepartmentId { get; set; }

        [Display(Name = "Field.Parent")]
        public Guid? ParentId { get; set; }

        [Display(Name = "Field.Phone")]
        [Phone(ErrorMessage = "Validation.PhoneInvalid")]
        public string? MobilePhone { get; set; }

        [Display(Name = "Field.AccountingCode")]
        public string? AccountingCode { get; set; }

        [Display(Name = "Field.Email")]
        [EmailAddress(ErrorMessage = "Validation.EmailInvalid")]
        public string? Email { get; set; }

        [Display(Name = "Field.Roles")]
        public List<Guid> SelectedRoleIds { get; set; } = new();
    }

    public class UserEditViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Validation.FirstNameRequired")]
        [Display(Name = "Field.FirstName")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation.LastNameRequired")]
        [Display(Name = "Field.LastName")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Field.UserName")]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "Field.Department")]
        public Guid? DepartmentId { get; set; }

        [Display(Name = "Field.Parent")]
        public Guid? ParentId { get; set; }

        [Display(Name = "Field.Phone")]
        [Phone(ErrorMessage = "Validation.PhoneInvalid")]
        public string? MobilePhone { get; set; }

        [Display(Name = "Field.AccountingCode")]
        public string? AccountingCode { get; set; }

        [Display(Name = "Field.Email")]
        [EmailAddress(ErrorMessage = "Validation.EmailInvalid")]
        public string? Email { get; set; }

        [Display(Name = "Field.Roles")]
        public List<Guid> SelectedRoleIds { get; set; } = new();
    }

    public class UserResetPasswordViewModel
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation.NewPasswordRequired")]
        [MinLength(6, ErrorMessage = "Validation.PasswordMinLength")]
        [DataType(DataType.Password)]
        [Display(Name = "Field.NewPassword")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation.ConfirmPasswordRequired")]
        [Compare(nameof(NewPassword), ErrorMessage = "Validation.PasswordsMustMatch")]
        [DataType(DataType.Password)]
        [Display(Name = "Field.ConfirmPassword")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ProfileViewModel
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public string? ParentName { get; set; }
        public DateTime CreatedOn { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<string> Authorities { get; set; } = new();

        [Display(Name = "Field.Phone")]
        [Phone(ErrorMessage = "Validation.PhoneInvalid")]
        public string? MobilePhone { get; set; }

        [Display(Name = "Field.AccountingCode")]
        public string? AccountingCode { get; set; }

        [Display(Name = "Field.Email")]
        [EmailAddress(ErrorMessage = "Validation.EmailInvalid")]
        public string? Email { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Validation.CurrentPasswordRequired")]
        [DataType(DataType.Password)]
        [Display(Name = "Field.CurrentPassword")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation.NewPasswordRequired")]
        [MinLength(6, ErrorMessage = "Validation.PasswordMinLength")]
        [DataType(DataType.Password)]
        [Display(Name = "Field.NewPassword")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation.ConfirmPasswordRequired")]
        [Compare(nameof(NewPassword), ErrorMessage = "Validation.PasswordsMustMatch")]
        [DataType(DataType.Password)]
        [Display(Name = "Field.ConfirmPassword")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
