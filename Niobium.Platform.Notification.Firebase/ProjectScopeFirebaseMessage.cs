namespace Niobium.Platform.Notification.Firebase
{
    public class ProjectScopeFirebaseMessage
    {
        public required string ProjectID { get; set; }

        public required FirebaseMessage Message { get; set; }
    }
}
