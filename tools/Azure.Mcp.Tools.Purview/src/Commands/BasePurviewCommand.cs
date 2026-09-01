// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using Azure.Mcp.Tools.Purview.Services;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.Purview.Commands;

public abstract class BasePurviewCommand<
    [DynamicallyAccessedMembers(TrimAnnotations.CommandAnnotations)] TOptions, TResult>
    : AuthenticatedCommand<TOptions, TResult> where TOptions : class
{
    protected override void HandleException(CommandContext context, Exception ex)
    {
        var safeException = ex switch
        {
            CommandValidationException => ex,
            PurviewServiceException => ex,
            ArgumentException => new HttpRequestException("The Microsoft Purview request contains invalid input.", null, HttpStatusCode.BadRequest),
            _ => new HttpRequestException("The Microsoft Purview request failed.", null, HttpStatusCode.InternalServerError)
        };

        base.HandleException(context, safeException);
        context.Response.Results = null;
    }

    protected override string GetErrorMessage(Exception ex) => ex.Message;
}
