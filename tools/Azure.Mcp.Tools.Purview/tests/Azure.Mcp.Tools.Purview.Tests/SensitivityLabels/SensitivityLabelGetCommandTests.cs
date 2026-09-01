// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Azure.Mcp.Tools.Purview.Commands.SensitivityLabels;
using Azure.Mcp.Tools.Purview.Models.SensitivityLabels;
using Azure.Mcp.Tools.Purview.Options.SensitivityLabels;
using Azure.Mcp.Tools.Purview.Services;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Tests.Client;
using Microsoft.Purview.SDK.Models.Labels;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Azure.Mcp.Tools.Purview.Tests.SensitivityLabels;

public class SensitivityLabelGetCommandTests : CommandUnitTestsBase<SensitivityLabelGetCommand, IPurviewService>
{
    private const string LabelId = "00000000-0000-0000-0000-000000000003";
    private const string Tenant = "00000000-0000-0000-0000-000000000001";
    private const string UserEmail = "user@contoso.com";

    [Fact]
    public void Constructor_InitializesCommandCorrectly()
    {
        var command = Command.GetCommand();

        Assert.Equal("get", command.Name);
        Assert.NotNull(command.Description);
        Assert.NotEmpty(command.Description);
    }

    [Theory]
    [InlineData("not-an-email", null, "en-US", "--user-email must be a valid email address")]
    [InlineData("user@contoso.com", "not-a-guid", "en-US", "--label-ids must contain valid sensitivity label IDs")]
    [InlineData("user@contoso.com", null, "not_a_locale", "--locale must be a valid locale name")]
    public void ValidateOptions_InvalidInput_ReturnsValidationError(
        string userEmail,
        string? labelId,
        string locale,
        string expectedError)
    {
        var options = new SensitivityLabelGetOptions
        {
            UserEmail = userEmail,
            LabelIds = labelId is null ? null : [labelId],
            Locale = locale,
            Tenant = Tenant
        };
        var validationResult = new ValidationResult();

        Command.ValidateOptions(options, validationResult);

        Assert.Contains(validationResult.Errors, error => error.Contains(expectedError, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("--tenant tenant-id")]
    [InlineData("--user-email user@contoso.com")]
    public async Task ExecuteAsync_MissingRequiredOption_ReturnsBadRequest(string args)
    {
        var response = await ExecuteCommandAsync(args);

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
    }

    [Fact]
    public async Task ExecuteAsync_ValidInput_ReturnsLabelsAndForwardsOptions()
    {
        var child = CreateLabel("child-id", "Child", null);
        var expected = CreateLabel(
            LabelId,
            "Confidential",
            new(LabelId, "owner@contoso.com", UserEmail, "View, Edit"),
            [child]);
        Service.GetSensitivityLabelsAsync(
            Tenant,
            UserEmail,
            Arg.Any<string[]?>(),
            SensitivityLabelTarget.File,
            "en-US",
            Arg.Any<CancellationToken>())
            .Returns(new[] { expected });

        var response = await ExecuteCommandAsync(
            "--tenant", Tenant,
            "--user-email", UserEmail,
            "--label-ids", LabelId,
            "--content-target", "File");

        var result = ValidateAndDeserializeResponse(response, PurviewJsonContext.Default.SensitivityLabelGetCommandResult);
        var label = Assert.Single(result.Labels);
        Assert.Equal("Confidential", label.DisplayName);
        Assert.Equal("View, Edit", label.Rights?.UsageRights);
        Assert.Equal("Child", Assert.Single(label.Sublabels).DisplayName);
        await Service.Received(1).GetSensitivityLabelsAsync(
            Tenant,
            UserEmail,
            Arg.Is<string[]>(ids => ids.SequenceEqual(new[] { LabelId })),
            SensitivityLabelTarget.File,
            "en-US",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ServiceError_DoesNotExposeBackendDetails()
    {
        const string SensitiveDetail = "sensitive-user@contoso.com";
        Service.GetSensitivityLabelsAsync(
            Tenant,
            UserEmail,
            Arg.Any<string[]?>(),
            Arg.Any<SensitivityLabelTarget?>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception(SensitiveDetail));

        var response = await ExecuteCommandAsync("--tenant", Tenant, "--user-email", UserEmail);

        Assert.Equal(HttpStatusCode.InternalServerError, response.Status);
        Assert.DoesNotContain(SensitiveDetail, response.Message);
        Assert.Null(response.Results);
    }

    private static SensitivityLabelInfo CreateLabel(
        string id,
        string displayName,
        SensitivityLabelRightsInfo? rights,
        IReadOnlyCollection<SensitivityLabelInfo>? sublabels = null) => new(
            Id: id,
            Name: displayName,
            DisplayName: displayName,
            Description: "Label description",
            ToolTip: "Label tooltip",
            Color: "#ff0000",
            Priority: 1,
            Sensitivity: null,
            IsDefault: false,
            HasProtection: true,
            IsEnabled: true,
            ApplicableTo: "File",
            ActionSource: "Manual",
            ContentFormats: ["File"],
            Rights: rights,
            Sublabels: sublabels ?? []);
}
