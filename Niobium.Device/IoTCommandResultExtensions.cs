namespace Niobium.Device
{
    public static class IoTCommandResultExtensions
    {
        public static T GetPayload<T>(this IoTCommandResult result)
        {
            return JsonMarshaller.Unmarshall<T>(result.PayloadJSON)!;
        }
    }
}
