namespace SafeHome.API.Options
{
    public class SocialNetworkOption
    {
        public string Name { get; set; } = string.Empty;
        public string ApiUrl { get; set; } = string.Empty;
        public string? ApiKey { get; set; }
        public bool Enabled { get; set; } = true;
    }
}
