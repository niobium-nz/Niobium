namespace Cod.Identity
{
    public class AuthenticationResult
    {
        public bool IsSuccess => ChallengeSubject == null && App.HasValue && User.HasValue;

        public Guid? App { get; set; }

        public Guid? User { get; set; }

        public AuthenticationKind? Challenge { get; set; }

        public string? ChallengeSubject { get; set; }
    }
}
