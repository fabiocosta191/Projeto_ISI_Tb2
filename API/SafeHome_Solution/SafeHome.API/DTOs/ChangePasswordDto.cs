using System.ComponentModel.DataAnnotations;

namespace SafeHome.API.DTOs
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "A password atual é obrigatória.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "A nova password é obrigatória.")]
        [MinLength(6, ErrorMessage = "A password deve ter pelo menos 6 caracteres.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
