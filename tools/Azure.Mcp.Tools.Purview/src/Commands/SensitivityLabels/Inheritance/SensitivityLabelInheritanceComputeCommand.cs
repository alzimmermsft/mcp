// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tools.Purview.Models.SensitivityLabels;
using Azure.Mcp.Tools.Purview.Options.SensitivityLabels.Inheritance;
using Azure.Mcp.Tools.Purview.Services;
using Azure.Mcp.Tools.Purview.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.Purview.Commands.SensitivityLabels.Inheritance;

[CommandMetadata(
    Id = "25793028-1550-4c84-9ad0-70515d5bf7ea",
    Name = "compute",
    Title = "Compute Sensitivity Label Inheritance",
    Description = """
        Computes the effective Microsoft Purview sensitivity label inherited from one or more source labels for a user.
        Requires --tenant, --user-email, and --label-ids. Optionally accepts source content formats and locale.
        """,
    Destructive = false,
    Idempotent = true,
    OpenWorld = false,
    ReadOnly = true,
    Secret = true,
    LocalRequired = false)]
public sealed class SensitivityLabelInheritanceComputeCommand(
    ILogger<SensitivityLabelInheritanceComputeCommand> logger,
    IPurviewService service)
    : BasePurviewCommand<SensitivityLabelInheritanceComputeOptions, SensitivityLabelInheritanceComputeCommand.SensitivityLabelInheritanceComputeCommandResult>
{
    private readonly ILogger<SensitivityLabelInheritanceComputeCommand> _logger = logger;
    private readonly IPurviewService _service = service;

    public override void ValidateOptions(SensitivityLabelInheritanceComputeOptions options, ValidationResult validationResult)
    {
        base.ValidateOptions(options, validationResult);
        SensitivityLabelOptionValidation.ValidateUserEmail(options.UserEmail, validationResult);
        SensitivityLabelOptionValidation.ValidateLabelIds(options.LabelIds, validationResult);
        SensitivityLabelOptionValidation.ValidateContentFormats(options.ContentFormats, validationResult);
        SensitivityLabelOptionValidation.ValidateLocale(options.Locale, validationResult);
    }

    public override async Task<CommandResponse> ExecuteAsync(
        CommandContext context,
        SensitivityLabelInheritanceComputeOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var label = await _service.ComputeSensitivityLabelInheritanceAsync(
                options.Tenant,
                options.UserEmail,
                options.LabelIds,
                options.ContentFormats,
                options.Locale,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new(label),
                PurviewJsonContext.Default.SensitivityLabelInheritanceComputeCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Error computing sensitivity label inheritance. FailureType: {FailureType}, LabelIdCount: {LabelIdCount}, ContentFormatCount: {ContentFormatCount}.",
                ex.GetType().Name,
                options.LabelIds.Length,
                options.ContentFormats?.Length ?? 0);
            HandleException(context, ex);
        }

        return context.Response;
    }

    public sealed record SensitivityLabelInheritanceComputeCommandResult(SensitivityLabelInfo Label);
}
