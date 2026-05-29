// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Tools.Purview.Commands.ProtectionScopes;

namespace Azure.Mcp.Tools.Purview;

[JsonSerializable(typeof(ComputeCommand.ComputeResults))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
internal sealed partial class PurviewJsonContext : JsonSerializerContext;
