namespace Cod.Platform
{
    public abstract class Endpoints
    {
        public const string ParameterPaymentServiceProvider = "{paymentServiceProvider}";

        public const string TemplateWindcaveNotification = "v1/" + ParameterPaymentServiceProvider + "/notifications";
        public static readonly EndpointFormat FormatWindcaveNotification = new EndpointFormat(TemplateWindcaveNotification);
    }
}
