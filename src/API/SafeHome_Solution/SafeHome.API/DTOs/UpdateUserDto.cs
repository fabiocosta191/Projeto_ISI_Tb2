namespace SafeHome.API.DTOs
{
    public class UpdateUserDto
    {
        public string Role { get; set; } = "User";
        public string? NewPassword { get; set; }
    }
}
