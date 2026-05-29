// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Graph.Models;

namespace Azure.Mcp.Tools.Purview.Services;

/// <summary>
/// Service interface for Microsoft Purview operations.
/// </summary>
public interface IPurviewService
{
    Task<List<PolicyUserScope>?> ComputeProtectionScopesAsync(
        string userId,
        List<string>? activities = null,
        List<string>? policyLocations = null,
        string? tenantId = null,
        CancellationToken cancellationToken = default);
}
