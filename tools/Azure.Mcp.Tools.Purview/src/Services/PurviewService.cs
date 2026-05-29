// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Services.Azure;
using Azure.Mcp.Core.Services.Azure.Tenant;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Mcp.Core.Services.Azure.Authentication;

namespace Azure.Mcp.Tools.Purview.Services;

/// <summary>
/// Service implementation for Microsoft Purview operations.
/// </summary>
public sealed class PurviewService(ITenantService tenantService, ILogger<PurviewService> logger)
    : BaseAzureService(tenantService), IPurviewService
{
    private readonly ILogger<PurviewService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<List<PolicyUserScope>?> ComputeProtectionScopesAsync(
        string userId,
        List<string>? activities = null,
        List<string>? policyLocations = null,
        string? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var tokenCredential = await GetCredential(tenantId, cancellationToken);
        var httpClient = TenantService.GetClient();
        httpClient.BaseAddress = GetBaseAddress();
        using var graphClient = new GraphServiceClient(httpClient, tokenCredential);

        var response = await graphClient.Users[userId].DataSecurityAndGovernance.ProtectionScopes.Compute.PostAsComputePostResponseAsync(new()
        {
            Activities = ConvertToUserActivityTypes(activities),
            Locations = ConvertToPolicyLocations(policyLocations)
        }, cancellationToken: cancellationToken);

        return response?.Value;
    }

    private static UserActivityTypes? ConvertToUserActivityTypes(List<string>? activities)
    {
        if (activities == null || activities.Count == 0)
        {
            return null;
        }

        UserActivityTypes result = 0;
        foreach (var activity in activities)
        {
            if (Enum.TryParse<UserActivityTypes>(activity, ignoreCase: true, out var parsed))
            {
                result |= parsed;
            }
            else
            {
                throw new ArgumentException($"Invalid activity type: {activity}. Valid values are 'uploadText', 'downloadText', etc. (case-insensitive).");
            }
        }
        return result;
    }

    private static List<PolicyLocation>? ConvertToPolicyLocations(List<string>? policyLocations)
    {
        if (policyLocations == null)
        {
            return null;
        }

        var locations = new List<PolicyLocation>();
        foreach (var loc in policyLocations)
        {
            var parts = loc.Split(':', 2);
            if (parts.Length != 2)
            {
                throw new ArgumentException($"Invalid policy location format: {loc}. Expected format is 'kind:location'.");
            }

            var kind = parts[0].Trim();
            var location = parts[1].Trim();
            PolicyLocation policyLocation = kind.ToLower() switch
            {
                "policylocationapplication" => new PolicyLocationApplication { Value = location },
                "policylocationdomain" => new PolicyLocationDomain { Value = location },
                "policylocationurl" => new PolicyLocationUrl { Value = location },
                _ => throw new ArgumentException($"Invalid policy location kind: {kind}. Valid kinds are 'policyLocationApplication', 'policyLocationDomain', 'policyLocationUrl' (case-insensitive).")
            };

            locations.Add(policyLocation);
        }
        return locations;
    }

    private Uri GetBaseAddress() => TenantService.CloudConfiguration.CloudType switch
    {
        AzureCloudConfiguration.AzureCloud.AzurePublicCloud => new("https://graph.microsoft.com/v1/"),
        AzureCloudConfiguration.AzureCloud.AzureChinaCloud => new("https://microsoftgraph.chinacloudapi.cn/v1/"),
        AzureCloudConfiguration.AzureCloud.AzureUSGovernmentCloud => new("https://graph.microsoft.us/v1/"),
        _ => throw new NotSupportedException($"The cloud type {TenantService.CloudConfiguration.CloudType} is not supported.")
    };
}
