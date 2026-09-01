// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;

namespace Azure.Mcp.Tools.Purview.Services;

internal sealed class PurviewServiceException(string message, HttpStatusCode statusCode)
    : HttpRequestException(message, inner: null, statusCode);
