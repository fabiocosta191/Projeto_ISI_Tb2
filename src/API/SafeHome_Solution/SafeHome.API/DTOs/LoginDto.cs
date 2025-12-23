using System.ComponentModel.DataAnnotations; // Necessário para [Required]

namespace SafeHome.API.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "O username é obrigatório.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "A password é obrigatória.")]
        public string Password { get; set; }
    }
}