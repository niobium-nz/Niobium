namespace Cod
{
    public interface ITrackable
    {
        DateTimeOffset? Timestamp { get; set; }

        DateTimeOffset? Created { get; set; }
    }
}
