// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tools.Purview.Commands.ProtectionScopes;
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

        services.AddSingleton<ComputeCommand>();
    }

    public CommandGroup RegisterCommands(IServiceProvider serviceProvider)
    {
        var purview = new CommandGroup(
            Name,
            """
            Microsoft Purview operations - Manage and interact with Microsoft Purview resources, including computing
            protection scopes.
            """,
            Title);

        var protectionScopes = new CommandGroup(
            "protection-scopes",
            "Commands for computing protection scopes for users in Microsoft Purview.");
        protectionScopes.AddCommand(serviceProvider.GetRequiredService<ComputeCommand>());
        purview.AddSubGroup(protectionScopes);

        return purview;
    }
}
