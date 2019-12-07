namespace Cod.Channel
{
    public interface ICommandService
    {
        ICommand Get(string commandID);
    }
}
