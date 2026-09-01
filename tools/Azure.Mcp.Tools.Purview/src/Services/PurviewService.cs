// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Azure.Mcp.Core.Services.Azure;
using Azure.Mcp.Tools.Purview.Models.ProtectionScopes;
using Azure.Mcp.Tools.Purview.Models.SensitivityLabels;
using Microsoft.DataClassification.Client.Exceptions;
using Microsoft.Mcp.Core.Services.Azure.Authentication;
using Microsoft.Purview.SDK.Client;
using Microsoft.Purview.SDK.ClientSettings;
using Microsoft.Purview.SDK.Models.Labels;
using Microsoft.Purview.SDK.Models.ProcessContent;
using Microsoft.Purview.SDK.Models.ProtectionScopes;
using Microsoft.Purview.SDK.Models.Requests;
using Microsoft.Purview.SDK.Models.Responses;

namespace Azure.Mcp.Tools.Purview.Services;

/// <summary>
/// Service implementation for Microsoft Purview operations.
/// </summary>
public sealed class PurviewService(IAzureService azureService)
    : BaseAzureService(azureService), IPurviewService
{
    public async Task<IReadOnlyCollection<SensitivityLabelInfo>> GetSensitivityLabelsAsync(
        string tenant,
        string userEmail,
        string[]? labelIds = null,
        SensitivityLabelTarget? contentTarget = null,
        string locale = "en-US",
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters((nameof(tenant), tenant), (nameof(userEmail), userEmail), (nameof(locale), locale));

        var resolvedTenantId = await AzureService.GetTenantId(tenant, cancellationToken);
        PurviewClientSettings settings = new()
        {
            GraphServiceBaseUri = GetSensitivityLabelBaseAddress(),
            UserAgent = UserAgent
        };
        using var client = new PurviewClient(settings);
        using var httpClient = AzureService.GetClient();
        var requestOptions = await CreateRequestOptionsAsync(resolvedTenantId, httpClient, cancellationToken);
        SensitivityLabelAndRightsRequest request = new(Guid.NewGuid())
        {
            TenantId = resolvedTenantId,
            DelegatedUserEmail = userEmail,
            ScopeToUser = true,
            GetRights = true,
            Locale = locale,
            ContentFormats = contentTarget,
            LabelIds = labelIds?.Select(Guid.Parse).ToArray()
        };

        try
        {
            var labels = await client.GetSensitivityLabelsAndRightsAsync(request, requestOptions, cancellationToken);
            return labels.Select(ConvertSensitivityLabel).ToArray();
        }
        catch (PurviewHttpRequestException ex)
        {
            throw CreateServiceException(ex, "The Microsoft Purview sensitivity label request failed.");
        }
    }

    public async Task<SensitivityLabelRightsResult> ComputeSensitivityLabelRightsAsync(
        string tenant,
        string userEmail,
        string labelId,
        string contentFormat,
        string contentId,
        string locale = "en-US",
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(tenant), tenant),
            (nameof(userEmail), userEmail),
            (nameof(labelId), labelId),
            (nameof(contentFormat), contentFormat),
            (nameof(contentId), contentId),
            (nameof(locale), locale));

        var resolvedTenantId = await AzureService.GetTenantId(tenant, cancellationToken);
        PurviewClientSettings settings = new()
        {
            GraphServiceBaseUri = GetSensitivityLabelBaseAddress(),
            UserAgent = UserAgent
        };
        using var client = new PurviewClient(settings);
        using var httpClient = AzureService.GetClient();
        var requestOptions = await CreateRequestOptionsAsync(resolvedTenantId, httpClient, cancellationToken);
        ComputeRightsAndInheritanceRequest request = new(
            [new ProtectedContentItem(labelId, contentFormat, contentId)],
            Guid.NewGuid())
        {
            TenantId = resolvedTenantId,
            DelegatedUserEmail = userEmail,
            Locale = locale,
            SupportedContentFormats = [contentFormat]
        };

        try
        {
            var response = await client.ComputeRightsAndInheritanceGraphAsync(request, requestOptions, cancellationToken);
            return new(
                response.InheritedLabel is null ? null : ConvertSensitivityLabel(response.InheritedLabel),
                response.SensitivityLabels?.Select(ConvertSensitivityLabel).ToArray() ?? [],
                response.ContentRights?.Select(static rights => new ProtectedContentRightsInfo(
                    rights.ContentId,
                    rights.ContentFormat,
                    rights.Label is null ? null : ConvertSensitivityLabel(rights.Label),
                    rights.Rights?.Select(static right => right.ToString()).ToArray() ?? [])).ToArray() ?? []);
        }
        catch (PurviewHttpRequestException ex)
        {
            throw CreateServiceException(ex, "The Microsoft Purview sensitivity label rights request failed.");
        }
    }

    public async Task<SensitivityLabelInfo> ComputeSensitivityLabelInheritanceAsync(
        string tenant,
        string userEmail,
        string[] labelIds,
        string[]? contentFormats = null,
        string locale = "en-US",
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters((nameof(tenant), tenant), (nameof(userEmail), userEmail), (nameof(locale), locale));
        ArgumentNullException.ThrowIfNull(labelIds);

        var resolvedTenantId = await AzureService.GetTenantId(tenant, cancellationToken);
        PurviewClientSettings settings = new()
        {
            GraphServiceBaseUri = GetSensitivityLabelBaseAddress(),
            UserAgent = UserAgent
        };
        using var client = new PurviewClient(settings);
        using var httpClient = AzureService.GetClient();
        var requestOptions = await CreateRequestOptionsAsync(resolvedTenantId, httpClient, cancellationToken);
        LabelInheritanceRequest request = new(
            resolvedTenantId,
            labelIds,
            userEmail,
            locale,
            Guid.NewGuid())
        {
            ContentFormats = contentFormats
        };

        try
        {
            var label = await client.ComputeInheritanceGraphAsync(request, requestOptions, cancellationToken);
            return ConvertSensitivityLabel(label);
        }
        catch (PurviewHttpRequestException ex)
        {
            throw CreateServiceException(ex, "The Microsoft Purview sensitivity label inheritance request failed.");
        }
    }

    public async Task<ProtectionScopesResult> ComputeProtectionScopesAsync(
        string tenant,
        string? userId = null,
        string[]? activities = null,
        string[]? policyLocations = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters((nameof(tenant), tenant));

        var resolvedTenantId = await AzureService.GetTenantId(tenant, cancellationToken);
        PurviewClientSettings settings = new()
        {
            GraphServiceBaseUri = GetProtectionScopesBaseAddress(),
            UserAgent = UserAgent
        };
        using var client = new PurviewClient(settings);
        using var httpClient = AzureService.GetClient();

        ProtectionScopesRequest request = new();
        var convertedActivities = ConvertToUserActivityTypes(activities);
        if (convertedActivities != null)
        {
            request.Activities = (ProtectionScopeActivities)convertedActivities;
        }
        var locations = ConvertToPolicyLocations(policyLocations);
        if (locations != null)
        {
            request.Locations = locations;
        }

        var requestOptions = await CreateRequestOptionsAsync(resolvedTenantId, httpClient, cancellationToken);

        try
        {
            if (userId is not null)
            {
                var response = await client.SearchUserProtectionScopeAsync(request, resolvedTenantId, userId, requestOptions, cancellationToken);
                return new("user", response.ScopeIdentifier, response.Scopes?.Select(ConvertUserScope).ToArray() ?? []);
            }

            var tenantResponse = await client.SearchTenantProtectionScopeAsync(request, resolvedTenantId, requestOptions, cancellationToken);
            return new("tenant", tenantResponse.ScopeIdentifier, tenantResponse.Scopes?.Select(ConvertTenantScope).ToArray() ?? []);
        }
        catch (PurviewHttpRequestException ex)
        {
            throw CreateServiceException(ex, "The Microsoft Purview protection scopes request failed.");
        }
    }

    private async Task<ClientRequestOptions> CreateRequestOptionsAsync(
        string tenantId,
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        var tokenCredential = await AzureService.GetTokenCredentialAsync(tenantId, cancellationToken);
        return new(async (context, requestCancellationToken) =>
        {
            var accessToken = await tokenCredential.GetTokenAsync(new(
                scopes: context.Scopes.ToArray(),
                claims: context.Claims,
                tenantId: context.TenantId,
                requestUri: context.Authority), requestCancellationToken);
            return accessToken.Token;
        })
        {
            HttpMiddleware = (httpRequest, requestCancellationToken, _) =>
                httpClient.SendAsync(httpRequest, requestCancellationToken)
        };
    }

    private static PurviewServiceException CreateServiceException(PurviewHttpRequestException ex, string fallbackMessage)
    {
        var statusCode = ex.HttpStatusCode ?? ex.StatusCode;
        if (statusCode is null && ex.Message.Contains("InsufficientGraphPermissions", StringComparison.OrdinalIgnoreCase))
        {
            statusCode = HttpStatusCode.Forbidden;
        }

        var resolvedStatusCode = statusCode ?? HttpStatusCode.ServiceUnavailable;
        var message = resolvedStatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
            ? "Microsoft Purview denied the request. Verify that the signed-in identity has the required Microsoft Graph permissions and Purview license."
            : fallbackMessage;
        return new(message, resolvedStatusCode);
    }

    private static SensitivityLabelInfo ConvertSensitivityLabel(SensitivityLabelResponse label) => new(
        label.Id,
        label.Name,
        label.DisplayName,
        label.Description,
        label.ToolTip,
        label.Color,
        label.Priority,
        null,
        label.IsDefault,
        label.HasProtection,
        label.IsEnabled,
        label.ApplicableTo?.ToString(),
        label.ActionSource?.ToString(),
        label.ApplicableTo is null ? [] : [label.ApplicableTo.Value.ToString()],
        label.Rights is null
            ? null
            : new(label.Rights.Id, label.Rights.OwnerEmail, label.Rights.UserEmail, label.Rights.Value.ToString()),
        label.Sublabels?.Select(ConvertSensitivityLabel).ToArray() ?? []);

    private static SensitivityLabelInfo ConvertSensitivityLabel(SensitivityLabel label) => new(
        label.Id,
        label.Name,
        null,
        label.Description,
        label.Tooltip,
        label.Color,
        null,
        label.Sensitivity,
        label.IsDefault,
        label.HasProtection,
        label.IsActive,
        null,
        label.ActionSource.ToString(),
        label.ContentFormats?.ToArray() ?? [],
        null,
        label.Children?.Select(ConvertSensitivityLabel).ToArray() ?? []);

    private static ProtectionScopeInfo ConvertUserScope(PolicyUserScope scope) => new(
        scope.Activities,
        scope.ExecutionMode,
        ConvertLocations(scope.Locations),
        scope.PolicyActions?.Select(static action => action.Action).ToArray() ?? []);

    private static ProtectionScopeInfo ConvertTenantScope(PolicyTenantScope scope) => new(
        scope.Activities,
        scope.ExecutionMode,
        ConvertLocations(scope.Locations),
        scope.PolicyActions?.Select(static action => action.Action).ToArray() ?? [],
        scope.PolicyScope is null
            ? null
            : new(
                ConvertBindingEntries(scope.PolicyScope.Inclusions),
                ConvertBindingEntries(scope.PolicyScope.Exclusions)));

    private static IReadOnlyCollection<ProtectionScopeLocationInfo> ConvertLocations(IEnumerable<PolicyLocation>? locations) =>
        locations?.Select(static location => new ProtectionScopeLocationInfo(location.DataType, location.Value)).ToArray() ?? [];

    private static IReadOnlyCollection<ProtectionScopeBindingEntryInfo> ConvertBindingEntries(IEnumerable<ScopeBase>? entries) =>
        entries?.Select(static entry => new ProtectionScopeBindingEntryInfo(entry.DataType, entry.Identity)).ToArray() ?? [];

    private static ProtectionScopeActivities? ConvertToUserActivityTypes(string[]? activities)
    {
        if (activities == null || activities.Length == 0)
        {
            return null;
        }

        ProtectionScopeActivities result = 0;
        foreach (var activity in activities)
        {
            result |= activity.ToLowerInvariant() switch
            {
                "uploadtext" => ProtectionScopeActivities.UploadText,
                "uploadfile" => ProtectionScopeActivities.UploadFile,
                "downloadtext" => ProtectionScopeActivities.DownloadText,
                "downloadfile" => ProtectionScopeActivities.DownloadFile,
                _ => throw new ArgumentException(
                    "Invalid activity type. Valid values are 'UploadText', 'UploadFile', 'DownloadText', and 'DownloadFile' (case-insensitive).",
                    nameof(activities))
            };
        }
        return result;
    }

    private static List<PolicyLocation>? ConvertToPolicyLocations(string[]? policyLocations)
    {
        if (policyLocations == null || policyLocations.Length == 0)
        {
            return null;
        }

        var locations = new List<PolicyLocation>();
        foreach (var loc in policyLocations)
        {
            var parts = loc.Split(':', 2);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[1]))
            {
                throw new ArgumentException("Invalid policy location. Expected format is 'kind:location' with a non-empty location.", nameof(policyLocations));
            }

            var kind = parts[0].Trim();
            var location = parts[1].Trim();
            PolicyLocation policyLocation = kind.ToLowerInvariant() switch
            {
                "policylocationapplication" => new PolicyLocationApplication(location),
                "policylocationdomain" => new PolicyLocationDomain(location),
                "policylocationurl" => new PolicyLocationUrl(location),
                _ => throw new ArgumentException(
                    "Invalid policy location kind. Valid kinds are 'policyLocationApplication', 'policyLocationDomain', and 'policyLocationUrl' (case-insensitive).",
                    nameof(policyLocations))
            };

            locations.Add(policyLocation);
        }
        return locations;
    }

    private Uri GetProtectionScopesBaseAddress() => AzureService.CloudConfiguration.CloudType switch
    {
        AzureCloudConfiguration.AzureCloud.AzurePublicCloud => new("https://graph.microsoft.com/v1.0/"),
        AzureCloudConfiguration.AzureCloud.AzureChinaCloud => new("https://microsoftgraph.chinacloudapi.cn/v1.0/"),
        AzureCloudConfiguration.AzureCloud.AzureUSGovernmentCloud => new("https://graph.microsoft.us/v1.0/"),
        _ => throw new NotSupportedException($"The cloud type {AzureService.CloudConfiguration.CloudType} is not supported.")
    };

    private Uri GetSensitivityLabelBaseAddress() => AzureService.CloudConfiguration.CloudType switch
    {
        AzureCloudConfiguration.AzureCloud.AzurePublicCloud => new("https://graph.microsoft.com/beta/"),
        _ => throw new PurviewServiceException(
            "Microsoft Purview sensitivity label operations are currently supported only in the Azure public cloud.",
            HttpStatusCode.UnprocessableEntity)
    };
}
