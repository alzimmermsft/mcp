// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Azure.Mcp.Tools.Purview.Commands.SensitivityLabels.Rights;
using Azure.Mcp.Tools.Purview.Models.SensitivityLabels;
using Azure.Mcp.Tools.Purview.Options.SensitivityLabels.Rights;
using Azure.Mcp.Tools.Purview.Services;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Tests.Client;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Azure.Mcp.Tools.Purview.Tests.SensitivityLabels.Rights;

public class SensitivityLabelRightsComputeCommandTests
    : CommandUnitTestsBase<SensitivityLabelRightsComputeCommand, IPurviewService>
{
    private const string ContentFormat = "File";
    private const string ContentId = "opaque-content-id";
    private const string LabelId = "00000000-0000-0000-0000-000000000003";
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
    [InlineData("not-an-email", "00000000-0000-0000-0000-000000000003", "File", "content", "en-US", "--user-email")]
    [InlineData("user@contoso.com", "not-a-guid", "File", "content", "en-US", "--label-ids")]
    [InlineData("user@contoso.com", "00000000-0000-0000-0000-000000000003", "File", "content", "bad_locale", "--locale")]
    public void ValidateOptions_InvalidInput_ReturnsValidationError(
        string userEmail,
        string labelId,
        string contentFormat,
        string contentId,
        string locale,
        string expectedOption)
    {
        var options = new SensitivityLabelRightsComputeOptions
        {
            UserEmail = userEmail,
            LabelId = labelId,
            ContentFormat = contentFormat,
            ContentId = contentId,
            Locale = locale,
            Tenant = Tenant
        };
        var validationResult = new ValidationResult();

        Command.ValidateOptions(options, validationResult);

        Assert.Contains(validationResult.Errors, error => error.Contains(expectedOption, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("--tenant tenant-id --user-email user@contoso.com")]
    [InlineData("--label-id 00000000-0000-0000-0000-000000000003 --content-format File --content-id item")]
    public async Task ExecuteAsync_MissingRequiredOptions_ReturnsBadRequest(string args)
    {
        var response = await ExecuteCommandAsync(args);

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
    }

    [Fact]
    public async Task ExecuteAsync_ValidInput_ReturnsRightsAndForwardsOptions()
    {
        var label = CreateLabel();
        var expected = new SensitivityLabelRightsResult(
            label,
            [label],
            [new(ContentId, ContentFormat, label, ["VIEW", "EDIT"])]);
        Service.ComputeSensitivityLabelRightsAsync(
            Tenant,
            UserEmail,
            LabelId,
            ContentFormat,
            ContentId,
            "en-US",
            Arg.Any<CancellationToken>())
            .Returns(expected);

        var response = await ExecuteCommandAsync(
            "--tenant", Tenant,
            "--user-email", UserEmail,
            "--label-id", LabelId,
            "--content-format", ContentFormat,
            "--content-id", ContentId);

        var result = ValidateAndDeserializeResponse(
            response,
            PurviewJsonContext.Default.SensitivityLabelRightsComputeCommandResult);
        Assert.Equal(LabelId, result.InheritedLabel?.Id);
        Assert.Equal(LabelId, Assert.Single(result.SensitivityLabels).Id);
        var contentRights = Assert.Single(result.ContentRights);
        Assert.Equal(ContentId, contentRights.ContentId);
        Assert.Equal(["VIEW", "EDIT"], contentRights.Rights);
        await Service.Received(1).ComputeSensitivityLabelRightsAsync(
            Tenant,
            UserEmail,
            LabelId,
            ContentFormat,
            ContentId,
            "en-US",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ServiceError_DoesNotExposeBackendDetails()
    {
        const string SensitiveDetail = "sensitive-content-detail";
        Service.ComputeSensitivityLabelRightsAsync(
            Tenant,
            UserEmail,
            LabelId,
            ContentFormat,
            ContentId,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception(SensitiveDetail));

        var response = await ExecuteCommandAsync(
            "--tenant", Tenant,
            "--user-email", UserEmail,
            "--label-id", LabelId,
            "--content-format", ContentFormat,
            "--content-id", ContentId);

        Assert.Equal(HttpStatusCode.InternalServerError, response.Status);
        Assert.DoesNotContain(SensitiveDetail, response.Message);
        Assert.Null(response.Results);
    }

    private static SensitivityLabelInfo CreateLabel() => new(
        Id: LabelId,
        Name: "Confidential",
        DisplayName: "Confidential",
        Description: "Label description",
        ToolTip: "Label tooltip",
        Color: "#ff0000",
        Priority: null,
        Sensitivity: 3,
        IsDefault: false,
        HasProtection: true,
        IsEnabled: true,
        ApplicableTo: null,
        ActionSource: "Manual",
        ContentFormats: [ContentFormat],
        Rights: null,
        Sublabels: []);
}
