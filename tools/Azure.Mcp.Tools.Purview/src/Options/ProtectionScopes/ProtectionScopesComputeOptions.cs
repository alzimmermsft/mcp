// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Options;
using Microsoft.Mcp.Core.Options;

namespace Azure.Mcp.Tools.Purview.Options.ProtectionScopes;

public sealed class ProtectionScopesComputeOptions
{
    [Option(Description = "The optional Microsoft Entra user object ID to compute user-scoped protection scopes for. When omitted, computes tenant-level protection scopes.")]
    public string? UserId { get; set; }

    [Option(Description = "The activities to compute protection scopes on (e.g., 'uploadText', 'downloadText').")]
    public string[]? Activities { get; set; }

    [Option(Description = "A list of policy locations in the format 'kind:location' (e.g., 'policyLocationApplication:83ef208a-0396-4893-9d4f-d36efbffc8bd', 'policyLocationDomain:domain.com', 'policyLocationUrl:https://subdomain.domain.com').")]
    public string[]? PolicyLocations { get; set; }

    [Option(Description = OptionDescriptions.Tenant)]
    public required string Tenant { get; set; }
}
