namespace Cod.Channel.Profile
{
    public class ProfileOptions
    {
        public const string DefaultHttpClientName = "IdentityAPIClient";

        public required string ProfileServiceHost { get; set; }

        public required string ProfileServiceEndpoint { get; set; } = Cod.Profile.Constants.DefaultProfileEndpoint;

        public void Validate()
        {
            ArgumentNullException.ThrowIfNull(ProfileServiceHost, nameof(ProfileServiceHost));
            ProfileServiceHost = ProfileServiceHost.Trim();
            if (ProfileServiceHost.EndsWith('/'))
            {
                ProfileServiceHost = ProfileServiceHost.Remove(ProfileServiceHost.Length - 1);
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
