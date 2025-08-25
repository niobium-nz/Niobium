using System.ComponentModel.DataAnnotations;

namespace Niobium.Channel.Identity
{
    public class TOTPLoginInput : IUserInput
    {
        [Required]
        public string? Username { get; set; }

        public string? Password { get; set; }

        public bool IsRemember { get; set; }

        public void Sanitize()
        {
            if (Username != null)
            {
                Username = Username.Trim();
            }

            if (Password != null)
            {
                Password = Password.Trim();
            }
        }
    }
}
