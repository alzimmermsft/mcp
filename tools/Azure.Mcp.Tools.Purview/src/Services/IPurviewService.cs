// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tools.Purview.Models.ProtectionScopes;
using Azure.Mcp.Tools.Purview.Models.SensitivityLabels;
using Microsoft.Purview.SDK.Models.Labels;

namespace Azure.Mcp.Tools.Purview.Services;

/// <summary>
/// Service interface for Microsoft Purview operations.
/// </summary>
public interface IPurviewService
{
    Task<IReadOnlyCollection<SensitivityLabelInfo>> GetSensitivityLabelsAsync(
        string tenant,
        string userEmail,
        string[]? labelIds = null,
        SensitivityLabelTarget? contentTarget = null,
        string locale = "en-US",
        CancellationToken cancellationToken = default);

    Task<SensitivityLabelRightsResult> ComputeSensitivityLabelRightsAsync(
        string tenant,
        string userEmail,
        string labelId,
        string contentFormat,
        string contentId,
        string locale = "en-US",
        CancellationToken cancellationToken = default);

    Task<SensitivityLabelInfo> ComputeSensitivityLabelInheritanceAsync(
        string tenant,
        string userEmail,
        string[] labelIds,
        string[]? contentFormats = null,
        string locale = "en-US",
        CancellationToken cancellationToken = default);

    Task<ProtectionScopesResult> ComputeProtectionScopesAsync(
        string tenant,
        string? userId = null,
        string[]? activities = null,
        string[]? policyLocations = null,
        CancellationToken cancellationToken = default);
}
