// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tools.Purview.Commands.ProtectionScopes;
using Azure.Mcp.Tools.Purview.Commands.SensitivityLabels;
using Azure.Mcp.Tools.Purview.Commands.SensitivityLabels.Inheritance;
using Azure.Mcp.Tools.Purview.Commands.SensitivityLabels.Rights;
using Azure.Mcp.Tools.Purview.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Mcp.Core.Areas;
using Microsoft.Mcp.Core.Commands;

namespace Azure.Mcp.Tools.Purview;

/// <summary>
/// Setup class for the Purview toolset.
/// </summary>
public class PurviewSetup : IAreaSetup
{
    public string Name => "purview";

    public string Title => "Microsoft Purview";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IPurviewService, PurviewService>();

        services.AddSingleton<ProtectionScopesComputeCommand>();
        services.AddSingleton<SensitivityLabelGetCommand>();
        services.AddSingleton<SensitivityLabelInheritanceComputeCommand>();
        services.AddSingleton<SensitivityLabelRightsComputeCommand>();
    }

    public CommandGroup RegisterCommands(IServiceProvider serviceProvider)
    {
        var purview = new CommandGroup(
            Name,
            """
            Microsoft Purview operations - Compute protection scopes and retrieve or evaluate sensitivity labels,
            inherited labels, and usage rights.
            """,
            Title);

        var protectionScopes = new CommandGroup(
            "protectionscopes",
            "Commands for computing user-scoped or tenant-level protection scopes in Microsoft Purview.");
        protectionScopes.AddCommand(serviceProvider.GetRequiredService<ProtectionScopesComputeCommand>());
        purview.AddSubGroup(protectionScopes);

        var sensitivityLabel = new CommandGroup(
            "sensitivitylabel",
            "Commands for retrieving and computing Microsoft Purview sensitivity labels and rights.");
        sensitivityLabel.AddCommand(serviceProvider.GetRequiredService<SensitivityLabelGetCommand>());

        var sensitivityLabelRights = new CommandGroup(
            "rights",
            "Commands for computing usage rights for Microsoft Purview sensitivity labels.");
        sensitivityLabelRights.AddCommand(serviceProvider.GetRequiredService<SensitivityLabelRightsComputeCommand>());
        sensitivityLabel.AddSubGroup(sensitivityLabelRights);

        var sensitivityLabelInheritance = new CommandGroup(
            "inheritance",
            "Commands for computing inheritance for Microsoft Purview sensitivity labels.");
        sensitivityLabelInheritance.AddCommand(serviceProvider.GetRequiredService<SensitivityLabelInheritanceComputeCommand>());
        sensitivityLabel.AddSubGroup(sensitivityLabelInheritance);
        purview.AddSubGroup(sensitivityLabel);

        return purview;
    }
}
