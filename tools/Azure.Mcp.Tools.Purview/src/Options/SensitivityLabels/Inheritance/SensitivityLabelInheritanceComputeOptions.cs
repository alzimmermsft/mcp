// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Options;
using Microsoft.Mcp.Core.Options;

namespace Azure.Mcp.Tools.Purview.Options.SensitivityLabels.Inheritance;

public sealed class SensitivityLabelInheritanceComputeOptions
{
    [Option(Description = "The email address of the user for whom label inheritance should be computed.")]
    public required string UserEmail { get; set; }

    [Option(Description = "One or more sensitivity label IDs from the source content.")]
    public required string[] LabelIds { get; set; }

    [Option(Description = "Optional content formats represented by the source labels, such as File or Email.")]
    public string[]? ContentFormats { get; set; }

    [Option(Description = "The locale for the computed label name and description.", DefaultValue = "en-US")]
    public string Locale { get; set; } = "en-US";

    [Option(Description = OptionDescriptions.Tenant)]
    public required string Tenant { get; set; }
}
