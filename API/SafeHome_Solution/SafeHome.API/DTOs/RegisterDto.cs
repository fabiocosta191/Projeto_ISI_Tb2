using System.ComponentModel.DataAnnotations;

namespace SafeHome.API.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "O username é obrigatório.")]
        [MinLength(3, ErrorMessage = "O username deve ter pelo menos 3 caracteres.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "A password é obrigatória.")]
        [MinLength(6, ErrorMessage = "A password deve ter pelo menos 6 caracteres.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "O perfil é obrigatório.")]
        [RegularExpression("Admin|User", ErrorMessage = "O perfil deve ser 'Admin' ou 'User'.")]
        public string Role { get; set; } = "User";
    }
}
