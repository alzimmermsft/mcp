// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tools.Purview.Commands;
using Azure.Mcp.Tools.Purview.Models.ProtectionScopes;
using Azure.Mcp.Tools.Purview.Options.ProtectionScopes;
using Azure.Mcp.Tools.Purview.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Purview.SDK.Models.ProtectionScopes;

namespace Azure.Mcp.Tools.Purview.Commands.ProtectionScopes;

[CommandMetadata(
    Id = "2f5aa486-978f-4611-83e0-89969bfecfef",
    Name = "compute",
    Title = "Compute Protection Scopes",
    Description = """
        Computes Microsoft Purview protection scopes for a user or tenant for specified activities and optional policy
        locations. Provide --user-id for user-scoped computation or omit it for tenant-level computation. Returns the
        scope type, scope identifier, activities, execution mode, locations, policy actions, and tenant policy bindings.
        """,
    Destructive = false,
    Idempotent = true,
    OpenWorld = false,
    ReadOnly = true,
    Secret = true,
    LocalRequired = false)]
public sealed class ProtectionScopesComputeCommand(ILogger<ProtectionScopesComputeCommand> logger, IPurviewService service)
    : BasePurviewCommand<ProtectionScopesComputeOptions, ProtectionScopesComputeCommand.ProtectionScopesComputeCommandResult>
{
    private const string ValidActivityValues = "'UploadText', 'UploadFile', 'DownloadText', 'DownloadFile'";
    private static readonly HashSet<string> s_validActivities = new(
        [
            nameof(ProtectionScopeActivities.UploadText),
            nameof(ProtectionScopeActivities.UploadFile),
            nameof(ProtectionScopeActivities.DownloadText),
            nameof(ProtectionScopeActivities.DownloadFile)
        ],
        StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> s_validPolicyLocations = new(["policyLocationApplication", "policyLocationDomain", "policyLocationUrl"], StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<ProtectionScopesComputeCommand> _logger = logger;
    private readonly IPurviewService _service = service;

    public override void ValidateOptions(ProtectionScopesComputeOptions options, ValidationResult validationResult)
    {
        base.ValidateOptions(options, validationResult);
        if (options.UserId is not null && !Guid.TryParse(options.UserId, out _))
        {
            validationResult.Errors.Add("--user-id must be a valid Microsoft Entra user object ID (GUID).");
        }

        if (options.Activities is { Length: > 0 })
        {
            var invalidActivities = options.Activities
                .Where(activity => !s_validActivities.Contains(activity))
                .Select(activity => $"'{activity}'")
                .ToList();
            if (invalidActivities.Count > 0)
            {
                validationResult.Errors.Add($"Invalid activity types: {string.Join(", ", invalidActivities)}. Valid values are {ValidActivityValues} (case-insensitive).");
            }
        }

        if (options.PolicyLocations is { Length: > 0 })
        {
            var invalidLocations = options.PolicyLocations.Any(location =>
            {
                var parts = location.Split(':', 2);
                return parts.Length != 2
                    || !s_validPolicyLocations.Contains(parts[0].Trim())
                    || string.IsNullOrWhiteSpace(parts[1]);
            });
            if (invalidLocations)
            {
                validationResult.Errors.Add("Invalid policy locations. Valid format is 'type:value' where allowed types are 'policyLocationApplication', 'policyLocationDomain', 'policyLocationUrl' (case-insensitive) and value isn't empty.");
            }
        }
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ProtectionScopesComputeOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var scopes = await _service.ComputeProtectionScopesAsync(
                options.Tenant,
                options.UserId,
                options.Activities,
                options.PolicyLocations,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new(scopes.ScopeType, scopes.ScopeIdentifier, scopes.Scopes),
                PurviewJsonContext.Default.ProtectionScopesComputeCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Error computing protection scopes. FailureType: {FailureType}, ScopeType: {ScopeType}, ActivityCount: {ActivityCount}, PolicyLocationCount: {PolicyLocationCount}.",
                ex.GetType().Name, options.UserId is null ? "tenant" : "user", options.Activities?.Length ?? 0, options.PolicyLocations?.Length ?? 0);
            HandleException(context, ex);
        }

        return context.Response;
    }

    public sealed record ProtectionScopesComputeCommandResult(
        string ScopeType,
        string? ScopeIdentifier,
        IReadOnlyCollection<ProtectionScopeInfo> Scopes);
}
