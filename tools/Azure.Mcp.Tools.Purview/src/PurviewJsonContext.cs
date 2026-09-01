// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Tools.Purview.Commands.ProtectionScopes;
using Azure.Mcp.Tools.Purview.Commands.SensitivityLabels;
using Azure.Mcp.Tools.Purview.Commands.SensitivityLabels.Inheritance;
using Azure.Mcp.Tools.Purview.Commands.SensitivityLabels.Rights;
using Azure.Mcp.Tools.Purview.Models.ProtectionScopes;
using Azure.Mcp.Tools.Purview.Models.SensitivityLabels;

namespace Azure.Mcp.Tools.Purview;

[JsonSerializable(typeof(ProtectedContentRightsInfo))]
[JsonSerializable(typeof(ProtectionScopeBindingEntryInfo))]
[JsonSerializable(typeof(ProtectionScopeInfo))]
[JsonSerializable(typeof(ProtectionScopeLocationInfo))]
[JsonSerializable(typeof(ProtectionScopePolicyBindingInfo))]
[JsonSerializable(typeof(ProtectionScopesComputeCommand.ProtectionScopesComputeCommandResult))]
[JsonSerializable(typeof(ProtectionScopesResult))]
[JsonSerializable(typeof(SensitivityLabelGetCommand.SensitivityLabelGetCommandResult))]
[JsonSerializable(typeof(SensitivityLabelInfo))]
[JsonSerializable(typeof(SensitivityLabelInheritanceComputeCommand.SensitivityLabelInheritanceComputeCommandResult))]
[JsonSerializable(typeof(SensitivityLabelRightsComputeCommand.SensitivityLabelRightsComputeCommandResult))]
[JsonSerializable(typeof(SensitivityLabelRightsInfo))]
[JsonSerializable(typeof(SensitivityLabelRightsResult))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true
)]
internal sealed partial class PurviewJsonContext : JsonSerializerContext;
