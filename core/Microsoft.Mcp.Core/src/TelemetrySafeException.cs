// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Mcp.Core;

/// <summary>
/// An exception type that is safe to include in telemetry, as it does not include any sensitive information in its message.
/// </summary>
public sealed class TelemetrySafeException(Exception realException)
    : Exception(realException.Message, realException);
