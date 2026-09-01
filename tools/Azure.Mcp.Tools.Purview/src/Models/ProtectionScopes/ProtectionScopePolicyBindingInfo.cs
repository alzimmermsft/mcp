// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.Purview.Models.ProtectionScopes;

public sealed record ProtectionScopePolicyBindingInfo(
    IReadOnlyCollection<ProtectionScopeBindingEntryInfo> Inclusions,
    IReadOnlyCollection<ProtectionScopeBindingEntryInfo> Exclusions);
