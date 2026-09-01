// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.Purview.Models.SensitivityLabels;

public sealed record SensitivityLabelInfo(
    string? Id,
    string? Name,
    string? DisplayName,
    string? Description,
    string? ToolTip,
    string? Color,
    int? Priority,
    int? Sensitivity,
    bool IsDefault,
    bool HasProtection,
    bool? IsEnabled,
    string? ApplicableTo,
    string? ActionSource,
    IReadOnlyCollection<string> ContentFormats,
    SensitivityLabelRightsInfo? Rights,
    IReadOnlyCollection<SensitivityLabelInfo> Sublabels);
