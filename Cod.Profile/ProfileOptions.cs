namespace Cod.Profile
{
    public class ProfileOptions
    {
        public required string ProfileServiceHost { get; set; }

        public required string ProfileServiceEndpoint { get; set; } = Cod.Profile.Constants.DefaultProfileEndpoint;

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
