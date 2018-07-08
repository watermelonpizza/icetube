using Google.Apis.Util.Store;
using IceTube.DataModels;
using IceTube.Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IceTube
{
    public class EfGoogleDataStore : IDataStore, IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EfGoogleDataStore> _logger;

        public EfGoogleDataStore(ApplicationDbContext context, ILogger<EfGoogleDataStore> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ClearAsync()
        {
            _context.GoogleDataStores.RemoveRange(_context.GoogleDataStores);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Ef Google Data Store was cleared");
        }

        public async Task DeleteAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            var item = await _context.GoogleDataStores.FirstOrDefaultAsync(x => x.Key == key);

            if (item != null && item.SourceType == typeof(T).FullName)
            {
                _context.GoogleDataStores.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<T> GetAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key), "key cannot be null or empty");
            }

            var item = await _context.GoogleDataStores.FirstOrDefaultAsync(x => x.Key == key);

            if (item == null)
            {
                return default(T);
            }

            if (item.SourceType != typeof(T).FullName)
            {
                _logger.LogWarning("Attempted to get data for key '{key}', but there was a missmatch on the source type, expceted: {sourceType}, actual: {actualType}", key, item.SourceType, typeof(T).FullName);
            }

            return JsonConvert.DeserializeObject<T>(item.Data);
        }

        public async Task StoreAsync<T>(string key, T value)
        {
            var store = await _context.GoogleDataStores.FindAsync(key);
            if (store != null)
            {
                store.SourceType = typeof(T).FullName;
                store.Data = JsonConvert.SerializeObject(value);
            }
            else
            {
                var newdataStore = new GoogleDataStoreObject
                {
                    Key = key,
                    SourceType = typeof(T).FullName,
                    Data = JsonConvert.SerializeObject(value)
                };

                await _context.GoogleDataStores.AddAsync(newdataStore);
            }

            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
