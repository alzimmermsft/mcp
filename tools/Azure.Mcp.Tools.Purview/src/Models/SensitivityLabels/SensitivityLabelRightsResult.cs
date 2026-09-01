// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.Purview.Models.SensitivityLabels;

public sealed record SensitivityLabelRightsResult(
    SensitivityLabelInfo? InheritedLabel,
    IReadOnlyCollection<SensitivityLabelInfo> SensitivityLabels,
    IReadOnlyCollection<ProtectedContentRightsInfo> ContentRights);
