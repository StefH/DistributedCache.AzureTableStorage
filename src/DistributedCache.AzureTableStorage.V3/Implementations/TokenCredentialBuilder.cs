using System.Collections.Generic;
using Azure.Core;
using Azure.Identity;
using DistributedCache.AzureTableStorage.Options;

namespace DistributedCache.AzureTableStorage.Implementations;

internal static class TokenCredentialBuilder
{
    public static TokenCredential Build(AzureTableStorageCacheOptions options)
    {
        var sources = new List<TokenCredential>();

        // 1. If TenantId, ClientId, Username and Password are defined, use UsernamePasswordCredential
        if (!string.IsNullOrEmpty(options.TenantId) && !string.IsNullOrEmpty(options.ClientId) && !string.IsNullOrEmpty(options.Username) && !string.IsNullOrEmpty(options.Password))
        {
            sources.Add(new UsernamePasswordCredential(options.Username, options.Password, options.TenantId, options.ClientId));
        }

        // 2. If TenantId, ClientId and ClientSecret are defined, use ClientSecretCredential
        if (!string.IsNullOrEmpty(options.TenantId) && !string.IsNullOrEmpty(options.ClientId) && !string.IsNullOrEmpty(options.ClientSecret))
        {
            sources.Add(new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret));
        }

        // 3. If ClientId is defined, use DefaultAzureCredential with the ManagedIdentityClientId
        if (!string.IsNullOrEmpty(options.ClientId))
        {
            sources.Add(new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = options.ClientId }));
        }

        // 4. Always authenticate using DefaultAzureCredential
        sources.Add(new DefaultAzureCredential());

        return new ChainedTokenCredential(sources.ToArray());
    }
}