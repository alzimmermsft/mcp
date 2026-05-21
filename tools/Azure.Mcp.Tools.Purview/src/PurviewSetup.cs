// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
    }

    public CommandGroup RegisterCommands(IServiceProvider serviceProvider)
    {
        var purview = new CommandGroup(
            Name,
            """
            Microsoft Purview operations - Manage and interact with Microsoft Purview resources, including creating and
            configuring Purview accounts, scanning data sources, managing classifications and glossary terms, and querying the Purview catalog.
            """,
            Title);

        return purview;
    }
}
