using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ProyectoTeamXP.Extensions
{
    public static class DistributedCacheExtensions
    {
        private static readonly byte[] EncryptionKey = Encoding.UTF8.GetBytes("TeamXP2026SecureKey123456789012");
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("TeamXP2026IV1234");

        public static async Task SetObjectAsync<T>(
            this IDistributedCache cache,
            string key,
            T value,
            DistributedCacheEntryOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var json = JsonSerializer.Serialize(value);
            var plainBytes = Encoding.UTF8.GetBytes(json);
            var encryptedBytes = EncryptData(plainBytes);

            await cache.SetAsync(key, encryptedBytes, options ?? new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(10)
            }, cancellationToken);
        }

        public static async Task<T?> GetObjectAsync<T>(
            this IDistributedCache cache,
            string key,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var encryptedBytes = await cache.GetAsync(key, cancellationToken);

            if (encryptedBytes == null || encryptedBytes.Length == 0)
                return default;

            try
            {
                var plainBytes = DecryptData(encryptedBytes);
                var json = Encoding.UTF8.GetString(plainBytes);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception)
            {
                await cache.RemoveAsync(key, cancellationToken);
                return default;
            }
        }

        public static void SetObject<T>(
            this IDistributedCache cache,
            string key,
            T value,
            DistributedCacheEntryOptions? options = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var json = JsonSerializer.Serialize(value);
            var plainBytes = Encoding.UTF8.GetBytes(json);
            var encryptedBytes = EncryptData(plainBytes);

            cache.Set(key, encryptedBytes, options ?? new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(10)
            });
        }

        public static T? GetObject<T>(this IDistributedCache cache, string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var encryptedBytes = cache.Get(key);

            if (encryptedBytes == null || encryptedBytes.Length == 0)
                return default;

            try
            {
                var plainBytes = DecryptData(encryptedBytes);
                var json = Encoding.UTF8.GetString(plainBytes);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception)
            {
                cache.Remove(key);
                return default;
            }
        }

        private static byte[] EncryptData(byte[] plainData)
        {
            using var aes = Aes.Create();
            aes.Key = EncryptionKey;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(plainData, 0, plainData.Length);
        }

        private static byte[] DecryptData(byte[] encryptedData)
        {
            using var aes = Aes.Create();
            aes.Key = EncryptionKey;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
        }
    }
}
