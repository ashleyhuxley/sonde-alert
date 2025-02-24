using ElectricFox.SondeAlert.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace ElectricFox.SondeAlert.Redis
{
    public class NotificationCache
    {
        private readonly RedisOptions _options;

        public NotificationCache(IOptions<RedisOptions> options)
        {
            _options = options.Value;
        }

        public async Task<bool> ShouldSendSondeNotification(long chatId, string sondeSerial)
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(_options.RedisServer);
            IDatabase db = redis.GetDatabase();

            var key = $"sonde:{chatId}:{sondeSerial}";
            string? value = await db.StringGetAsync(key);
            return value is null;
        }

        public async Task SaveSondeNotification(long chatId, string sondeSerial)
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(_options.RedisServer);
            IDatabase db = redis.GetDatabase();

            var key = $"sonde:{chatId}:{sondeSerial}";

            await db.StringSetAsync(key, "1", TimeSpan.FromDays(3));
        }

        public async Task<bool> ShouldSendAprsNotification(string callsign, string messageId)
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(_options.RedisServer);
            IDatabase db = redis.GetDatabase();

            var key = $"aprs:{callsign}:{messageId}";
            string? value = await db.StringGetAsync(key);
            return value is null;
        }

        public async Task SaveAprsNotification(string callsign, string messageId)
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(_options.RedisServer);
            IDatabase db = redis.GetDatabase();

            var key = $"aprs:{callsign}:{messageId}";

            await db.StringSetAsync(key, "1");
        }
    }
}
