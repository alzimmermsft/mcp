// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.Purview.Services;

/// <summary>
/// Service interface for Microsoft Purview operations.
/// </summary>
public interface IPurviewService
{
    Task GetSensitivityLabelsAndRightsAsync(
        string purviewAccountName,
        string tenantId,
        CancellationToken cancellationToken = default);
}
