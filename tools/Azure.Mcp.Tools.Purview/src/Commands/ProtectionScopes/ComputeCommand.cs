// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
        Computes protection scopes for a user in Microsoft Purview. This operation analyzes the user's activities and
        generates protection scopes based on their interactions with sensitive data. The computed protection scopes
        can then be used to enforce data access policies and protect sensitive information within the organization.
        """,
    Destructive = false,
    Idempotent = false,
    OpenWorld = false,
    ReadOnly = true,
    Secret = false,
    LocalRequired = false)]
public sealed class ComputeCommand(ILogger<ComputeCommand> logger, IPurviewService service)
    : AuthenticatedCommand<ComputeOptions, ComputeCommand.ComputeResults>()
{
    private static readonly string s_validActivities = string.Join(", ", Enum.GetNames<ProtectionScopeActivities>().Select(a => $"'{a}'"));
    private static readonly HashSet<string> s_validPolicyLocations = new(["policyLocationApplication", "policyLocationDomain", "policyLocationUrl"], StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<ComputeCommand> _logger = logger;
    private readonly IPurviewService _service = service;


    public override void ValidateOptions(ComputeOptions options, ValidationResult validationResult)
    {
        base.ValidateOptions(options, validationResult);
        if (options.Activities is { Count: > 0 })
        {
            var invalidActivities = options.Activities.Where(activity => !Enum.TryParse<ProtectionScopeActivities>(activity, ignoreCase: true, out _))
                .Select(a => $"'{a}'")
                .ToList();
            if (invalidActivities.Count > 0)
            {
                validationResult.Errors.Add($"Invalid activity types: {string.Join(", ", invalidActivities)}. Valid values are {s_validActivities} (case-insensitive).");
            }
        }
        if (options.PolicyLocations is { Count: > 0 })
        {
            var invalidLocations = options.PolicyLocations.Any(location => {
                var parts = location.Split(':', 2);
                // Valid format is "type:value", where type is one of the valid policy locations and value is non-empty.
                return parts.Length != 2
                    || !s_validPolicyLocations.Contains(parts[0].Trim())
                    || string.IsNullOrWhiteSpace(parts[1]);
            });
            if (invalidLocations)
            {
                // Error here is a bit opaque just in case the value contains sensitive information, but it gives enough info for the user to correct the format.
                validationResult.Errors.Add("Invalid policy locations. Valid format is 'type:value' where allowed types are 'policyLocationApplication', 'policyLocationDomain', 'policyLocationUrl' (case-insensitive) and value isn't empty.");
            }
        }
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ComputeOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var scopes = await _service.ComputeProtectionScopesAsync(
                options.UserId,
                options.Activities,
                options.PolicyLocations,
                options.Tenant,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(new(scopes ?? []), PurviewJsonContext.Default.ComputeResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error computing user protection scopes. UserId: {UserId}, Activities: {Activities}, PolicyLocations: {PolicyLocations}.",
                options.UserId, options.Activities, options.PolicyLocations);
            HandleException(context, ex);
        }

        return context.Response;
    }

    public sealed record ComputeResults(IReadOnlyCollection<PolicyUserScope> Scopes);
}
