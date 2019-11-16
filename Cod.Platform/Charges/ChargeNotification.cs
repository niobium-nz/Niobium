namespace Cod.Platform.Charges
{
    public abstract class ChargeNotification
    {

        public ChargeNotification(string content)
        {
            this.ParseContent(content);
        }

        protected abstract void ParseContent(string content);
    }
}
