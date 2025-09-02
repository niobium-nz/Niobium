namespace Niobium.Profile
{
    public class ProfileOptions
    {
        public required string ProfileServiceHost { get; set; }

        public required string ProfileServiceEndpoint { get; set; } = Constants.DefaultProfileEndpoint;

        public string? ProfileAppID { get; set; }

        public void Validate()
        {
            ArgumentNullException.ThrowIfNull(ProfileServiceHost, nameof(ProfileServiceHost));
            ProfileServiceHost = ProfileServiceHost.Trim();
            if (ProfileServiceHost.EndsWith('/'))
            {
                ProfileServiceHost = ProfileServiceHost[..^1];
            }

            ArgumentNullException.ThrowIfNull(ProfileServiceEndpoint, nameof(ProfileServiceEndpoint));
            ProfileServiceEndpoint = ProfileServiceEndpoint.Trim();
            if (!ProfileServiceEndpoint.StartsWith('/'))
            {
                ProfileServiceEndpoint = $"/{ProfileServiceEndpoint}";
            }
        }
    }
}
