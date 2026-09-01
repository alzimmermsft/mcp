// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Purview.SDK.Models.DCS;
using Microsoft.Purview.SDK.Models.ProtectionScopes;

namespace Azure.Mcp.Tools.Purview.Models.ProtectionScopes;

public sealed record ProtectionScopeInfo(
    ProtectionScopeActivities Activities,
    ExecutionMode ExecutionMode,
    IReadOnlyCollection<ProtectionScopeLocationInfo> Locations,
    IReadOnlyCollection<DlpAction> PolicyActions,
    ProtectionScopePolicyBindingInfo? PolicyScope = null);
