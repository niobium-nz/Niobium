namespace Cod.Platform.Messaging
{
    public interface IIoTCommander
    {
        Task<IoTCommandResult> ExecuteAsync(string device, object command, bool fireAndForget = true);
    }
}
