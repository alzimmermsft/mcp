// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Options;
using Microsoft.Mcp.Core.Options;

namespace Azure.Mcp.Tools.Purview.Options.SensitivityLabels;

public sealed class SensitivityLabelGetOptions
{
    [Option(Description = "The email address of the user whose available sensitivity labels should be retrieved.")]
    public required string UserEmail { get; set; }

    [Option(Description = "Optional sensitivity label IDs to retrieve. When omitted, returns all labels available to the user.")]
    public string[]? LabelIds { get; set; }

    [Option(Description = "Optional content target to filter labels for: Email, Site, UnifiedGroup, Teamwork, File, or SchematizedData.")]
    public SensitivityLabelContentTarget? ContentTarget { get; set; }

    [Option(Description = "The locale for label names and descriptions.", DefaultValue = "en-US")]
    public string Locale { get; set; } = "en-US";

    [Option(Description = OptionDescriptions.Tenant)]
    public required string Tenant { get; set; }
}
