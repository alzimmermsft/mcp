// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.Purview.Models.ProtectionScopes;

public sealed record ProtectionScopesResult(
    string ScopeType,
    string? ScopeIdentifier,
    IReadOnlyCollection<ProtectionScopeInfo> Scopes);
