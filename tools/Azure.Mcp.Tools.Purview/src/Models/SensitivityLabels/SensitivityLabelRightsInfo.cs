// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.Purview.Models.SensitivityLabels;

public sealed record SensitivityLabelRightsInfo(
    string? Id,
    string? OwnerEmail,
    string? UserEmail,
    string UsageRights);
