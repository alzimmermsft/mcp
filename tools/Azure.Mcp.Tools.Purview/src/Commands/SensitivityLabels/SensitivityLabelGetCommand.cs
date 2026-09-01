// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tools.Purview.Models.SensitivityLabels;
using Azure.Mcp.Tools.Purview.Options.SensitivityLabels;
using Azure.Mcp.Tools.Purview.Services;
using Azure.Mcp.Tools.Purview.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Purview.SDK.Models.Labels;

namespace Azure.Mcp.Tools.Purview.Commands.SensitivityLabels;

[CommandMetadata(
    Id = "dd8cab24-fa9d-4830-8924-6423404ffd29",
    Name = "get",
    Title = "Get Sensitivity Labels",
    Description = """
        Retrieves Microsoft Purview sensitivity labels available to a user, including effective rights and sublabels.
        Requires --tenant and --user-email. Optionally filters by label IDs and content target.
        """,
    Destructive = false,
    Idempotent = true,
    OpenWorld = false,
    ReadOnly = true,
    Secret = true,
    LocalRequired = false)]
public sealed class SensitivityLabelGetCommand(
    ILogger<SensitivityLabelGetCommand> logger,
    IPurviewService service)
    : BasePurviewCommand<SensitivityLabelGetOptions, SensitivityLabelGetCommand.SensitivityLabelGetCommandResult>
{
    private readonly ILogger<SensitivityLabelGetCommand> _logger = logger;
    private readonly IPurviewService _service = service;

    public override void ValidateOptions(SensitivityLabelGetOptions options, ValidationResult validationResult)
    {
        base.ValidateOptions(options, validationResult);
        SensitivityLabelOptionValidation.ValidateUserEmail(options.UserEmail, validationResult);
        SensitivityLabelOptionValidation.ValidateLabelIds(options.LabelIds, validationResult);
        SensitivityLabelOptionValidation.ValidateLocale(options.Locale, validationResult);
    }

    public override async Task<CommandResponse> ExecuteAsync(
        CommandContext context,
        SensitivityLabelGetOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var labels = await _service.GetSensitivityLabelsAsync(
                options.Tenant,
                options.UserEmail,
                options.LabelIds,
                ConvertContentTarget(options.ContentTarget),
                options.Locale,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new(labels),
                PurviewJsonContext.Default.SensitivityLabelGetCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Error retrieving sensitivity labels. FailureType: {FailureType}, LabelIdCount: {LabelIdCount}, ContentTarget: {ContentTarget}.",
                ex.GetType().Name,
                options.LabelIds?.Length ?? 0,
                options.ContentTarget);
            HandleException(context, ex);
        }

        return context.Response;
    }

    private static SensitivityLabelTarget? ConvertContentTarget(SensitivityLabelContentTarget? contentTarget) => contentTarget switch
    {
        SensitivityLabelContentTarget.Email => SensitivityLabelTarget.Email,
        SensitivityLabelContentTarget.Site => SensitivityLabelTarget.Site,
        SensitivityLabelContentTarget.UnifiedGroup => SensitivityLabelTarget.UnifiedGroup,
        SensitivityLabelContentTarget.Teamwork => SensitivityLabelTarget.Teamwork,
        SensitivityLabelContentTarget.File => SensitivityLabelTarget.File,
        SensitivityLabelContentTarget.SchematizedData => SensitivityLabelTarget.SchematizedData,
        null => null,
        _ => throw new ArgumentOutOfRangeException(nameof(contentTarget))
    };

    public sealed record SensitivityLabelGetCommandResult(IReadOnlyCollection<SensitivityLabelInfo> Labels);
}
