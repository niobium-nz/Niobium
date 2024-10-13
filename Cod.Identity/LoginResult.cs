namespace Cod.Identity
{
    public class LoginResult
    {
        public bool IsSuccess { get; set; }

        public AuthenticationKind? Challenge { get; set; }

        public string? ChallengeSubject { get; set; }
    }
}
