//using System.Threading.Tasks;
//using StackExchange.Redis;

//namespace Cod.Platform
//{
//    internal static class RedisClient
//    {
//        private static ConnectionMultiplexer connection;

//        public static async Task<IDatabase> GetDatabaseAsync()
//        {
//            var conn = await GetConnectionAsync();
//            return conn.GetDatabase();
//        }

//        public static async Task<IServer> GetServerAsync()
//        {
//            var connstr = await new ConfigurationProvider().GetSettingAsync("REDIS-CACHE");
//            var conn = await GetConnectionAsync();
//            return conn.GetServer(connstr.Split(',')[0]);
//        }

//        private static async Task<ConnectionMultiplexer> GetConnectionAsync()
//        {
//            if (connection == null)
//            {
//                var connstr = await new ConfigurationProvider().GetSettingAsync("REDIS-CACHE");
//                connection = ConnectionMultiplexer.Connect(connstr);
//            }
//            return connection;
//        }
//    }
//}
