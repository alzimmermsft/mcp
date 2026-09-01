// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Azure.Mcp.Tools.Purview.Commands.SensitivityLabels.Inheritance;
using Azure.Mcp.Tools.Purview.Models.SensitivityLabels;
using Azure.Mcp.Tools.Purview.Options.SensitivityLabels.Inheritance;
using Azure.Mcp.Tools.Purview.Services;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Tests.Client;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Azure.Mcp.Tools.Purview.Tests.SensitivityLabels.Inheritance;

public class SensitivityLabelInheritanceComputeCommandTests
    : CommandUnitTestsBase<SensitivityLabelInheritanceComputeCommand, IPurviewService>
{
    private const string FirstLabelId = "00000000-0000-0000-0000-000000000003";
    private const string SecondLabelId = "00000000-0000-0000-0000-000000000004";
    private const string Tenant = "00000000-0000-0000-0000-000000000001";
    private const string UserEmail = "user@contoso.com";

    [Fact]
    public void Constructor_InitializesCommandCorrectly()
    {
        var command = Command.GetCommand();

        Assert.Equal("compute", command.Name);
        Assert.NotNull(command.Description);
        Assert.NotEmpty(command.Description);
    }

    [Theory]
    [InlineData("not-an-email", "00000000-0000-0000-0000-000000000003", "en-US", "--user-email")]
    [InlineData("user@contoso.com", "not-a-guid", "en-US", "--label-ids")]
    [InlineData("user@contoso.com", "00000000-0000-0000-0000-000000000003", "bad_locale", "--locale")]
    public void ValidateOptions_InvalidInput_ReturnsValidationError(
        string userEmail,
        string labelId,
        string locale,
        string expectedOption)
    {
        var options = new SensitivityLabelInheritanceComputeOptions
        {
            UserEmail = userEmail,
            LabelIds = [labelId],
            Locale = locale,
            Tenant = Tenant
        };
        var validationResult = new ValidationResult();

        Command.ValidateOptions(options, validationResult);

        Assert.Contains(validationResult.Errors, error => error.Contains(expectedOption, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("--tenant tenant-id --user-email user@contoso.com")]
    [InlineData("--label-ids 00000000-0000-0000-0000-000000000003")]
    public async Task ExecuteAsync_MissingRequiredOptions_ReturnsBadRequest(string args)
    {
        var response = await ExecuteCommandAsync(args);

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
    }

    [Fact]
    public async Task ExecuteAsync_ValidInput_ReturnsInheritedLabelAndForwardsOptions()
    {
        string[] labelIds = [FirstLabelId, SecondLabelId];
        string[] contentFormats = ["File", "Email"];
        var expected = CreateLabel();
        Service.ComputeSensitivityLabelInheritanceAsync(
            Tenant,
            UserEmail,
            Arg.Any<string[]>(),
            Arg.Any<string[]?>(),
            "en-US",
            Arg.Any<CancellationToken>())
            .Returns(expected);

        var response = await ExecuteCommandAsync(
            "--tenant", Tenant,
            "--user-email", UserEmail,
            "--label-ids", labelIds[0], labelIds[1],
            "--content-formats", contentFormats[0], contentFormats[1]);

        var result = ValidateAndDeserializeResponse(
            response,
            PurviewJsonContext.Default.SensitivityLabelInheritanceComputeCommandResult);
        Assert.Equal(SecondLabelId, result.Label.Id);
        Assert.Equal("Highly Confidential", result.Label.Name);
        await Service.Received(1).ComputeSensitivityLabelInheritanceAsync(
            Tenant,
            UserEmail,
            Arg.Is<string[]>(ids => ids.SequenceEqual(labelIds)),
            Arg.Is<string[]>(formats => formats.SequenceEqual(contentFormats)),
            "en-US",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ServiceError_DoesNotExposeBackendDetails()
    {
        const string SensitiveDetail = "sensitive-label-detail";
        Service.ComputeSensitivityLabelInheritanceAsync(
            Tenant,
            UserEmail,
            Arg.Any<string[]>(),
            Arg.Any<string[]?>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception(SensitiveDetail));

        var response = await ExecuteCommandAsync(
            "--tenant", Tenant,
            "--user-email", UserEmail,
            "--label-ids", FirstLabelId);

        Assert.Equal(HttpStatusCode.InternalServerError, response.Status);
        Assert.DoesNotContain(SensitiveDetail, response.Message);
        Assert.Null(response.Results);
    }

    private static SensitivityLabelInfo CreateLabel() => new(
        Id: SecondLabelId,
        Name: "Highly Confidential",
        DisplayName: "Highly Confidential",
        Description: "Inherited label",
        ToolTip: "Inherited label tooltip",
        Color: "#ff0000",
        Priority: 4,
        Sensitivity: null,
        IsDefault: false,
        HasProtection: true,
        IsEnabled: true,
        ApplicableTo: "File",
        ActionSource: "Automatic",
        ContentFormats: ["File"],
        Rights: null,
        Sublabels: []);
}
