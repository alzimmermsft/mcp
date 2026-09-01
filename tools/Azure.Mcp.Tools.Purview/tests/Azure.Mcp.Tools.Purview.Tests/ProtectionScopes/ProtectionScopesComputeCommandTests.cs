// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Azure.Mcp.Tools.Purview.Commands.ProtectionScopes;
using Azure.Mcp.Tools.Purview.Models.ProtectionScopes;
using Azure.Mcp.Tools.Purview.Options.ProtectionScopes;
using Azure.Mcp.Tools.Purview.Services;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Tests.Client;
using Microsoft.Purview.SDK.Models.DCS;
using Microsoft.Purview.SDK.Models.ProcessContent;
using Microsoft.Purview.SDK.Models.ProtectionScopes;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Azure.Mcp.Tools.Purview.Tests.ProtectionScopes;

public class ProtectionScopesComputeCommandTests : CommandUnitTestsBase<ProtectionScopesComputeCommand, IPurviewService>
{
    private const string Tenant = "00000000-0000-0000-0000-000000000001";
    private const string UserId = "00000000-0000-0000-0000-000000000001";

    [Fact]
    public void Constructor_InitializesCommandCorrectly()
    {
        var command = Command.GetCommand();

        Assert.Equal("compute", command.Name);
        Assert.NotNull(command.Description);
        Assert.NotEmpty(command.Description);
    }

    [Fact]
    public void ValidateOptions_InvalidUserId_ReturnsValidationError()
    {
        var options = new ProtectionScopesComputeOptions
        {
            UserId = "not-a-guid",
            Tenant = Tenant
        };
        var validationResult = new ValidationResult();

        Command.ValidateOptions(options, validationResult);

        Assert.Single(validationResult.Errors);
        Assert.Contains("--user-id must be a valid Microsoft Entra user object ID", validationResult.Errors[0]);
    }

    [Theory]
    [InlineData("invalidActivity")]
    [InlineData("3")]
    [InlineData("None")]
    [InlineData("UnknownFutureValue")]
    public void ValidateOptions_InvalidActivities_ReturnsValidationError(string activity)
    {
        var options = new ProtectionScopesComputeOptions
        {
            UserId = UserId,
            Tenant = Tenant,
            Activities = [activity]
        };
        var validationResult = new ValidationResult();

        Command.ValidateOptions(options, validationResult);

        Assert.Single(validationResult.Errors);
        Assert.Contains($"Invalid activity types: '{activity}'.", validationResult.Errors[0]);
    }

    [Theory]
    [InlineData("invalidFormat")] // Missing colon
    [InlineData("policyLocationApplication:")] // Missing value
    [InlineData("policyLocationUnknown:value")] // Unknown location type
    public void ValidateOptions_InvalidPolicyLocations_ReturnsValidationError(string policyLocation)
    {
        var options = new ProtectionScopesComputeOptions
        {
            UserId = UserId,
            Tenant = Tenant,
            PolicyLocations = [policyLocation]
        };
        var validationResult = new ValidationResult();

        Command.ValidateOptions(options, validationResult);

        Assert.Single(validationResult.Errors);
        Assert.Contains("Invalid policy locations.", validationResult.Errors[0]);
    }

    [Theory]
    [InlineData("--user-id 00000000-0000-0000-0000-000000000001")]
    [InlineData("")]
    public async Task ExecuteAsync_MissingRequiredOption_ReturnsBadRequest(string args)
    {
        var response = await ExecuteCommandAsync(args);

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        Assert.Contains("required", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_UserScopeWithEmptyResults_ReturnsEmpty()
    {
        Service.ComputeProtectionScopesAsync(
            Tenant,
            UserId,
            null,
            null,
            Arg.Any<CancellationToken>())
            .Returns(new ProtectionScopesResult("user", "user-scope-id", []));

        var response = await ExecuteCommandAsync("--user-id", UserId, "--tenant", Tenant);

        var results = ValidateAndDeserializeResponse(response, PurviewJsonContext.Default.ProtectionScopesComputeCommandResult);
        Assert.Equal("user", results.ScopeType);
        Assert.Equal("user-scope-id", results.ScopeIdentifier);
        Assert.Empty(results.Scopes);
    }

    [Fact]
    public async Task ExecuteAsync_ValidInput_ReturnsResults()
    {
        string[] activities = ["uploadText", "downloadFile"];
        string[] policyLocations = ["policyLocationDomain:contoso.com"];
        var expectedLocation = new PolicyLocationApplication("83ef208a-0396-4893-9d4f-d36efbffc8bd");
        var expectedScope = new ProtectionScopeInfo(
            ProtectionScopeActivities.UploadText,
            ExecutionMode.EvaluateOffline,
            [new(expectedLocation.DataType, expectedLocation.Value)],
            [DlpAction.BlockAccess]);

        Service.ComputeProtectionScopesAsync(
            Tenant,
            UserId,
            Arg.Any<string[]?>(),
            Arg.Any<string[]?>(),
            Arg.Any<CancellationToken>())
            .Returns(new ProtectionScopesResult("user", "user-scope-id", [expectedScope]));

        var response = await ExecuteCommandAsync(
            "--user-id", UserId,
            "--tenant", Tenant,
            "--activities", activities[0], activities[1],
            "--policy-locations", policyLocations[0]);

        var results = ValidateAndDeserializeResponse(response, PurviewJsonContext.Default.ProtectionScopesComputeCommandResult);
        Assert.Equal("user", results.ScopeType);
        Assert.Equal("user-scope-id", results.ScopeIdentifier);
        var scope = Assert.Single(results.Scopes);
        Assert.Equal(expectedScope.Activities, scope.Activities);
        Assert.Equal(expectedScope.ExecutionMode, scope.ExecutionMode);
        var location = Assert.Single(scope.Locations);
        Assert.Equal(expectedLocation.DataType, location.DataType);
        Assert.Equal(expectedLocation.Value, location.Value);
        Assert.Equal(DlpAction.BlockAccess, Assert.Single(scope.PolicyActions));
        await Service.Received(1).ComputeProtectionScopesAsync(
            Tenant,
            UserId,
            Arg.Is<string[]>(value => value.SequenceEqual(activities)),
            Arg.Is<string[]>(value => value.SequenceEqual(policyLocations)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithoutUserId_ReturnsTenantScopesAndBindings()
    {
        var policyScope = new ProtectionScopePolicyBindingInfo(
            [new("group", "group-id")],
            [new("user", "excluded-user-id")]);
        var expectedScope = new ProtectionScopeInfo(
            ProtectionScopeActivities.UploadFile,
            ExecutionMode.EvaluateInline,
            [],
            [DlpAction.NotifyUser],
            policyScope);
        Service.ComputeProtectionScopesAsync(
            Tenant,
            null,
            null,
            null,
            Arg.Any<CancellationToken>())
            .Returns(new ProtectionScopesResult("tenant", "tenant-scope-id", [expectedScope]));

        var response = await ExecuteCommandAsync("--tenant", Tenant);

        var results = ValidateAndDeserializeResponse(response, PurviewJsonContext.Default.ProtectionScopesComputeCommandResult);
        Assert.Equal("tenant", results.ScopeType);
        Assert.Equal("tenant-scope-id", results.ScopeIdentifier);
        var scope = Assert.Single(results.Scopes);
        Assert.NotNull(scope.PolicyScope);
        Assert.Equal("group-id", Assert.Single(scope.PolicyScope.Inclusions).Identity);
        Assert.Equal("excluded-user-id", Assert.Single(scope.PolicyScope.Exclusions).Identity);
        await Service.Received(1).ComputeProtectionScopesAsync(
            Tenant,
            null,
            null,
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ServiceError_ReturnsInternalServerError()
    {
        const string SensitiveBackendDetail = "sensitive-user@contoso.com";
        Service.ComputeProtectionScopesAsync(
            Tenant,
            UserId,
            Arg.Any<string[]?>(),
            Arg.Any<string[]?>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception(SensitiveBackendDetail));

        var response = await ExecuteCommandAsync("--user-id", UserId, "--tenant", Tenant);

        Assert.Equal(HttpStatusCode.InternalServerError, response.Status);
        Assert.DoesNotContain(SensitiveBackendDetail, response.Message);
        Assert.Contains("Microsoft Purview request failed", response.Message);
        Assert.Contains("troubleshooting", response.Message);
        Assert.Null(response.Results);
    }
}
