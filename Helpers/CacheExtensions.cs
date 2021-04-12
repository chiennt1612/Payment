using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.Helpers
{
    public static class CacheExtensions
    {
        public static async Task<T> GetDataAsync<T>(this IDistributedCache _cache, string _key)
        {
            var key = DecryptorProvider.CreateMD5(_key);
            var b = await _cache.GetAsync(key);
            if (b != null)
            {
                if (b.Length > 0)
                {
                    return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(b));
                }
            }
            
            return default;
        }

        public static async Task SetDataAsync<T>(this IDistributedCache _cache, T data, string _key, int _timespan = 120)
        {
            var key = DecryptorProvider.CreateMD5(_key);
            var v = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
            var options = new DistributedCacheEntryOptions()
                               .SetSlidingExpiration(TimeSpan.FromSeconds(_timespan));
            await _cache.SetAsync(key, v, options);
        }

        public static async Task RemoveDataAsync(this IDistributedCache _cache, string _key)
        {
            var key = DecryptorProvider.CreateMD5(_key);
            await _cache.RemoveAsync(key);
        }
    }
}
