using System;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines.KeyValue.V2; 

namespace MyWeatherApp.Services
{
    public class VaultService
    {
        private readonly IVaultClient _vaultClient;
        private const string KvV2MountPoint = "secret"; 

        public VaultService()
        {
            string vaultAddress = Environment.GetEnvironmentVariable("VAULT_ADDR") ?? "http://localhost:8200";
            string vaultToken = Environment.GetEnvironmentVariable("VAULT_TOKEN") ?? "myroot";

            IAuthMethodInfo authMethod = new TokenAuthMethodInfo(vaultToken);
            var vaultClientSettings = new VaultClientSettings(vaultAddress, authMethod);
            _vaultClient = new VaultClient(vaultClientSettings);
        }

     
        public async Task<string> GetSecretAsync(string path, string key)
        {
      
            Secret<SecretData> secret = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: path, mountPoint: KvV2MountPoint);

            if (secret?.Data?.Data != null && secret.Data.Data.TryGetValue(key, out var value))
            {
                return value?.ToString();
            }
            return null;
        }
    }
}