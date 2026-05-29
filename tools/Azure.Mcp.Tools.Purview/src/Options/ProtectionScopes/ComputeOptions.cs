// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Options;
using Microsoft.Mcp.Core.Options;

namespace Azure.Mcp.Tools.Purview.Options.ProtectionScopes;

public sealed class ComputeOptions
{
    [Option("The user's Entra ID to compute protection scopres for.")]
    public required string UserId { get; set; }

    [Option("The activites to compute proctection scopes on (e.g., 'uploadText', 'downloadText').")]
    public List<string>? Activities { get; set; }

    [Option("A list of policy locations in the format 'kind:location' (e.g., 'policyLocationApplication:83ef208a-0396-4893-9d4f-d36efbffc8bd', 'policyLocationDomain:domain.com', 'policyLocationUrl:https://subdomain.domain.com').")]
    public List<string>? PolicyLocations { get; set; }

    [Option(OptionDescriptions.Tenant)]
    public string? Tenant { get; set; }
}
