// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.Purview.Models.SensitivityLabels;

public sealed record ProtectedContentRightsInfo(
    string? ContentId,
    string? ContentFormat,
    SensitivityLabelInfo? Label,
    IReadOnlyCollection<string> Rights);
