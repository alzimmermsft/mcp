// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tools.Purview.Commands.ProtectionScopes;
using Azure.Mcp.Tools.Purview.Options.ProtectionScopes;
using Azure.Mcp.Tools.Purview.Services;
using Microsoft.Graph.Models;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Tests.Client;
using NSubstitute;
using Xunit;

namespace Azure.Mcp.Tools.Purview.Tests.ProtectionScopes;

public class ComputeCommandTests : CommandUnitTestsBase<ComputeCommand, IPurviewService>
{
    [Fact]
    public void ValidateOptions_InvalidActivities_ReturnsValidationError()
    {
        // Arrange
        var options = new ComputeOptions
        {
            UserId = "user-id",
            Activities = ["invalidActivity"]
        };
        var validationResult = new ValidationResult();

        // Act
        Command.ValidateOptions(options, validationResult);

        // Assert
        Assert.Single(validationResult.Errors);
        Assert.Contains("Invalid activity types: 'invalidActivity'.", validationResult.Errors[0]);
    }

    [Theory]
    [InlineData("invalidFormat")] // Missing colon
    [InlineData("policyLocationApplication:")] // Missing value
    [InlineData("policyLocationUnknown:value")] // Unknown location type
    public void ValidateOptions_InvalidPolicyLocations_ReturnsValidationError(string policyLocation)
    {
        // Arrange
        var options = new ComputeOptions
        {
            UserId = "user-id",
            PolicyLocations = [policyLocation]
        };
        var validationResult = new ValidationResult();

        // Act
        Command.ValidateOptions(options, validationResult);

        // Assert
        Assert.Single(validationResult.Errors);
        Assert.Contains("Invalid policy locations.", validationResult.Errors[0]);
    }

    [Fact]
    public async Task ExecuteAsync_NullResults_ReturnsEmpty()
    {
        // Arrange
        var options = new ComputeOptions
        {
            UserId = "user-id"
        };
        Service.ComputeProtectionScopesAsync(
            options.UserId,
            options.Activities,
            options.PolicyLocations,
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns((List<PolicyUserScope>?)null);

        // Act
        var response = await ExecuteCommandAsync("--user-id", options.UserId);

        // Assert
        var results = ValidateAndDeserializeResponse(response, PurviewJsonContext.Default.ComputeResults);
        Assert.Empty(results.Scopes);
    }

    [Fact]
    public async Task ExecuteAsync_ValidInput_ReturnsResults()
    {
        // Arrange
        var options = new ComputeOptions
        {
            UserId = "user-id"
        };
        var expectedScope = new PolicyUserScope()
        {
            Activities = UserActivityTypes.UploadText,
            ExecutionMode = ExecutionMode.EvaluateOffline,
            Locations =
            [
                new PolicyLocationApplication
                {
                    Value = "83ef208a-0396-4893-9d4f-d36efbffc8bd"
                }
            ]
        };

        Service.ComputeProtectionScopesAsync(
            options.UserId,
            options.Activities,
            options.PolicyLocations,
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns([expectedScope]);

        // Act
        var response = await ExecuteCommandAsync("--user-id", options.UserId);

        // Assert
        var results = ValidateAndDeserializeResponse(response, PurviewJsonContext.Default.ComputeResults);
        Assert.Single(results.Scopes);
        Assert.Equal(expectedScope.Activities, results.Scopes[0].Activities);
        Assert.Equal(expectedScope.ExecutionMode, results.Scopes[0].ExecutionMode);
        Assert.NotNull(results.Scopes[0].Locations);
        Assert.Single(results.Scopes[0].Locations!);
        var location = results.Scopes[0].Locations![0];
        Assert.IsType<PolicyLocationApplication>(location);
        Assert.Equal(expectedScope.Locations[0].Value, location.Value);
    }
}
