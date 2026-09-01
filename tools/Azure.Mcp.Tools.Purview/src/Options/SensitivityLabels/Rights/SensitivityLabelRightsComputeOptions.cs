// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Options;
using Microsoft.Mcp.Core.Options;

namespace Azure.Mcp.Tools.Purview.Options.SensitivityLabels.Rights;

public sealed class SensitivityLabelRightsComputeOptions
{
    [Option(Description = "The email address of the user whose rights should be computed.")]
    public required string UserEmail { get; set; }

    [Option(Description = "The sensitivity label ID applied to the content.")]
    public required string LabelId { get; set; }

    [Option(Description = "The content format understood by Microsoft Purview, such as File or Email.")]
    public required string ContentFormat { get; set; }

    [Option(Description = "An opaque identifier for the content item. The content itself is not uploaded.")]
    public required string ContentId { get; set; }

    [Option(Description = "The locale for label names and descriptions.", DefaultValue = "en-US")]
    public string Locale { get; set; } = "en-US";

    [Option(Description = OptionDescriptions.Tenant)]
    public required string Tenant { get; set; }
}
