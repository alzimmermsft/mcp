// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Services.Azure;
using Azure.Mcp.Core.Services.Azure.Tenant;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Services.Azure.Authentication;
using Microsoft.Purview.SDK.Client;
using Microsoft.Purview.SDK.ClientSettings;
using Microsoft.Purview.SDK.Models.ProcessContent;
using Microsoft.Purview.SDK.Models.ProtectionScopes;
using Microsoft.Purview.SDK.Models.Requests;

namespace Azure.Mcp.Tools.Purview.Services;

/// <summary>
/// Service implementation for Microsoft Purview operations.
/// </summary>
public sealed class PurviewService(ITenantService tenantService)
    : BaseAzureService(tenantService), IPurviewService
{
    public async Task<IReadOnlyCollection<PolicyUserScope>?> ComputeProtectionScopesAsync(
        string userId,
        List<string>? activities = null,
        List<string>? policyLocations = null,
        string? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        PurviewClientSettings settings = new()
        {
            GraphServiceBaseUri = GetBaseAddress(),
            UserAgent = UserAgent,
            LoggingInjection = builder =>
            {
                builder.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
                builder.SetMinimumLevel(LogLevel.Information);
            }
        };
        var client = new PurviewClient(settings);

        ProtectionScopesRequest request = new();
        var convertedActivities = ConvertToUserActivityTypes(activities);
        if (convertedActivities != null)
        {
            request.Activities = (ProtectionScopeActivities)convertedActivities;
        }
        var locations = ConvertToPolicyLocations(policyLocations);
        if (locations != null)
        {
            request.Locations = locations!;
        }

        var tokenCredential = await GetCredential(tenantId, cancellationToken);
        ClientRequestOptions requestOptions = new(async (context, cancellation) =>
        {
            var accessToken = await tokenCredential.GetTokenAsync(new(
                scopes: context.Scopes.ToArray(),
                claims: context.Claims,
                tenantId: context.TenantId,
                requestUri: context.Authority), cancellation);
            return accessToken.Token;
        })
        {

        };

        var response = await client.SearchUserProtectionScopeAsync(request, tenantId, userId, requestOptions, cancellationToken);

        return response.Scopes;
    }

    private static ProtectionScopeActivities? ConvertToUserActivityTypes(List<string>? activities)
    {
        if (activities == null || activities.Count == 0)
        {
            return null;
        }

        ProtectionScopeActivities result = 0;
        foreach (var activity in activities)
        {
            if (Enum.TryParse<ProtectionScopeActivities>(activity, ignoreCase: true, out var parsed))
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
                "policylocationapplication" => new PolicyLocationApplication(location),
                "policylocationdomain" => new PolicyLocationDomain(location),
                "policylocationurl" => new PolicyLocationUrl(location),
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
