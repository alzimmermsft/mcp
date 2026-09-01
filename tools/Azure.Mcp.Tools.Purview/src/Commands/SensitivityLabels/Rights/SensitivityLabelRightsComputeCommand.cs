// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tools.Purview.Models.SensitivityLabels;
using Azure.Mcp.Tools.Purview.Options.SensitivityLabels.Rights;
using Azure.Mcp.Tools.Purview.Services;
using Azure.Mcp.Tools.Purview.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.Purview.Commands.SensitivityLabels.Rights;

[CommandMetadata(
    Id = "e6cae7a1-5742-4580-a3fa-81e0a6172ba0",
    Name = "compute",
    Title = "Compute Sensitivity Label Rights",
    Description = """
        Computes a user's Microsoft Purview usage rights and inherited sensitivity label for one labeled content item.
        Requires --tenant, --user-email, --label-id, --content-format, and --content-id. The content itself is not uploaded.
        """,
    Destructive = false,
    Idempotent = true,
    OpenWorld = false,
    ReadOnly = true,
    Secret = true,
    LocalRequired = false)]
public sealed class SensitivityLabelRightsComputeCommand(
    ILogger<SensitivityLabelRightsComputeCommand> logger,
    IPurviewService service)
    : BasePurviewCommand<SensitivityLabelRightsComputeOptions, SensitivityLabelRightsComputeCommand.SensitivityLabelRightsComputeCommandResult>
{
    private readonly ILogger<SensitivityLabelRightsComputeCommand> _logger = logger;
    private readonly IPurviewService _service = service;

    public override void ValidateOptions(SensitivityLabelRightsComputeOptions options, ValidationResult validationResult)
    {
        base.ValidateOptions(options, validationResult);
        SensitivityLabelOptionValidation.ValidateUserEmail(options.UserEmail, validationResult);
        SensitivityLabelOptionValidation.ValidateLabelIds([options.LabelId], validationResult);
        SensitivityLabelOptionValidation.ValidateContentFormat(options.ContentFormat, validationResult);
        SensitivityLabelOptionValidation.ValidateContentId(options.ContentId, validationResult);
        SensitivityLabelOptionValidation.ValidateLocale(options.Locale, validationResult);
    }

    public override async Task<CommandResponse> ExecuteAsync(
        CommandContext context,
        SensitivityLabelRightsComputeOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.ComputeSensitivityLabelRightsAsync(
                options.Tenant,
                options.UserEmail,
                options.LabelId,
                options.ContentFormat,
                options.ContentId,
                options.Locale,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new(result.InheritedLabel, result.SensitivityLabels, result.ContentRights),
                PurviewJsonContext.Default.SensitivityLabelRightsComputeCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Error computing sensitivity label rights. FailureType: {FailureType}, ContentFormatLength: {ContentFormatLength}.",
                ex.GetType().Name,
                options.ContentFormat.Length);
            HandleException(context, ex);
        }

        return context.Response;
    }

    public sealed record SensitivityLabelRightsComputeCommandResult(
        SensitivityLabelInfo? InheritedLabel,
        IReadOnlyCollection<SensitivityLabelInfo> SensitivityLabels,
        IReadOnlyCollection<ProtectedContentRightsInfo> ContentRights);
}
