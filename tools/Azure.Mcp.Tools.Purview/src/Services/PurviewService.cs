// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel.Primitives;
using Azure.Mcp.Core.Services.Azure;
using Azure.Mcp.Core.Services.Azure.Tenant;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Options;
using Microsoft.Purview.SDK.Client;
using Microsoft.Purview.SDK.ClientSettings;
using Microsoft.Purview.SDK.Models;
using Microsoft.Purview.SDK.Models.Requests;

namespace Azure.Mcp.Tools.Purview.Services;

/// <summary>
/// Service implementation for Microsoft Purview operations.
/// </summary>
public sealed class PurviewService(
    ITenantService tenantService,
    IHttpClientFactory httpClientFactory,
    ILogger<PurviewService> logger) : BaseAzureService(tenantService), IPurviewService
{
    public async Task GetSensitivityLabelsAndRightsAsync(
        string purviewAccountName,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        using var client = CreatePurviewClient();
        var credential = await GetCredential(tenantId, cancellationToken);
        credential.GetTokenAsync(new Azure.Core.TokenRequestContext())
        GetAzureCredentials(subscriptionId, tenantId);
        var requestOptions = new ClientRequestOptions
        {

            AuthTokenRetriever = ()
            CancellationToken = cancellationToken,
        };
        client.GetSensitivityLabelsAndRightsAsync()
        throw new NotImplementedException();
    }

    private PurviewClient CreatePurviewClient()
    {
        var settings = new PurviewClientSettings
        {
            TenantId = tenantId ?? TenantService.GetTenantId(tenantId),
        };
        var credentials = GetAzureCredentials(subscriptionId, tenantId);
        var httpClient = httpClientFactory.CreateClient();
        return new(settings);
    }

    private async ClientRequestOptions CreateClientRequestOptions(string tenantId, RetryPolicyOptions? retryPolicyOptions, CancellationToken cancellationToken)
    {
        var credential = await GetCredential(tenantId, cancellationToken);
        var requestOptions = new ClientRequestOptions();

        if (retryPolicyOptions != null)
        {
            if (retryPolicyOptions.MaxRetries.HasValue)
            {
                requestOptions.RetryOptions.MaxRetryAttempts = retryPolicyOptions.MaxRetries.Value;
            }
            if (retryPolicyOptions.DelaySeconds.HasValue)
            {
                requestOptions.RetryOptions.RetryWaitInMs = (int)(retryPolicyOptions.DelaySeconds.Value * 1000); // Convert seconds to milliseconds
            }
            if (retryPolicyOptions.NetworkTimeoutSeconds.HasValue)
            {
                requestOptions.RequestTimeoutInMilliseconds = (long)(retryPolicyOptions.NetworkTimeoutSeconds.Value * 1000); // Convert seconds to milliseconds
            }
        }

        return requestOptions;
    }
}
