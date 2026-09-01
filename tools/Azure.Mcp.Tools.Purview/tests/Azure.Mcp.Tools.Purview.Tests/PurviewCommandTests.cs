// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// cspell:ignore protectionscopes sensitivitylabel sublabels

using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Mcp.Tests;
using Microsoft.Mcp.Tests.Attributes;
using Microsoft.Mcp.Tests.Client;
using Microsoft.Mcp.Tests.Client.Helpers;
using Microsoft.Mcp.Tests.Generated.Models;
using Xunit;

namespace Azure.Mcp.Tools.Purview.Tests;

public sealed class PurviewCommandTests(
    ITestOutputHelper output,
    TestProxyFixture fixture,
    LiveServerFixture liveServerFixture)
    : RecordedCommandTestsBase(output, fixture, liveServerFixture)
{
    private const string ContentId = "purview-live-test-content";
    private const string HighPriorityLabelIdOutput = "PURVIEW_TEST_HIGH_PRIORITY_LABEL_ID";
    private const string LowPriorityLabelIdOutput = "PURVIEW_TEST_LOW_PRIORITY_LABEL_ID";
    private const string TestUserEmailOutput = "PURVIEW_TEST_USER_EMAIL";
    private const string TestUserIdOutput = "PURVIEW_TEST_USER_ID";
    private const string PlaybackTenantId = "00000000-0000-0000-0000-000000000000";
    private const string PlaybackTestUserId = "00000000-0000-0000-0000-000000000001";
    private const string PlaybackLowPriorityLabelId = "00000000-0000-0000-0000-000000000002";
    private const string PlaybackHighPriorityLabelId = "00000000-0000-0000-0000-000000000003";
    private const string PlaybackTestUserEmail = "purview-test-user@example.com";

    public override CustomDefaultMatcher? TestMatcher { get; set; } = new()
    {
        ExcludedHeaders = "Authorization,Client-Request-Id,x-ms-client-request-id"
    };

    protected override async ValueTask LoadSettingsAsync()
    {
        await base.LoadSettingsAsync();

        if (TestMode == Microsoft.Mcp.Tests.Helpers.TestMode.Playback)
        {
            return;
        }

        AddSanitizer(Settings.TenantId, PlaybackTenantId);
        AddDeploymentOutputSanitizer(TestUserIdOutput, PlaybackTestUserId);
        AddDeploymentOutputSanitizer(TestUserEmailOutput, PlaybackTestUserEmail);
        AddDeploymentOutputSanitizer(LowPriorityLabelIdOutput, PlaybackLowPriorityLabelId);
        AddDeploymentOutputSanitizer(HighPriorityLabelIdOutput, PlaybackHighPriorityLabelId);
    }

    [Fact]
    [LiveTestOnly]
    public async Task Should_compute_tenant_protection_scopes()
    {
        var result = await CallToolAsync(
            "purview_protectionscopes_compute",
            new()
            {
                { "tenant", GetTenantId() },
                { "activities", new[] { "UploadText", "DownloadText" } }
            });

        Assert.NotNull(result);
        Assert.Equal("tenant", result.Value.AssertProperty("scopeType").GetString());
        Assert.Equal(JsonValueKind.Array, result.Value.AssertProperty("scopes").ValueKind);
    }

    [Fact]
    [LiveTestOnly]
    public async Task Should_compute_user_protection_scopes()
    {
        var result = await CallToolAsync(
            "purview_protectionscopes_compute",
            new()
            {
                { "tenant", GetTenantId() },
                { "user-id", GetFixtureValue(TestUserIdOutput, PlaybackTestUserId) },
                { "activities", new[] { "UploadText", "DownloadText" } }
            });

        Assert.NotNull(result);
        Assert.Equal("user", result.Value.AssertProperty("scopeType").GetString());
        Assert.Equal(JsonValueKind.Array, result.Value.AssertProperty("scopes").ValueKind);
    }

    [Fact]
    [LiveTestOnly]
    public async Task Should_get_sensitivity_labels()
    {
        var expectedLabelIds = GetLabelIds();
        var result = await GetSensitivityLabelsAsync(expectedLabelIds);
        var labels = result.AssertProperty("labels");

        Assert.Equal(JsonValueKind.Array, labels.ValueKind);
        var actualLabelIds = EnumerateLabelIds(labels).ToArray();
        Assert.Contains(expectedLabelIds[0], actualLabelIds, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(expectedLabelIds[1], actualLabelIds, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    [LiveTestOnly]
    public async Task Should_compute_sensitivity_label_inheritance()
    {
        var labelIds = GetLabelIds();
        var result = await CallToolAsync(
            "purview_sensitivitylabel_inheritance_compute",
            new()
            {
                { "tenant", GetTenantId() },
                { "user-email", GetFixtureValue(TestUserEmailOutput, PlaybackTestUserEmail) },
                { "label-ids", labelIds },
                { "content-formats", new[] { "File" } }
            });

        Assert.NotNull(result);
        var label = result.Value.AssertProperty("label");
        Assert.Equal(labelIds[1], label.AssertProperty("id").GetString(), ignoreCase: true);
    }

    [Fact]
    [LiveTestOnly]
    public async Task Should_compute_sensitivity_label_rights()
    {
        var highPriorityLabelId = GetFixtureValue(HighPriorityLabelIdOutput, PlaybackHighPriorityLabelId);
        var result = await CallToolAsync(
            "purview_sensitivitylabel_rights_compute",
            new()
            {
                { "tenant", GetTenantId() },
                { "user-email", GetFixtureValue(TestUserEmailOutput, PlaybackTestUserEmail) },
                { "label-id", highPriorityLabelId },
                { "content-format", "File" },
                { "content-id", ContentId }
            });

        Assert.NotNull(result);
        var contentRights = Assert.Single(result.Value.AssertProperty("contentRights").EnumerateArray());
        Assert.Equal(ContentId, contentRights.AssertProperty("contentId").GetString());
        Assert.Equal(
            highPriorityLabelId,
            contentRights.AssertProperty("label").AssertProperty("id").GetString(),
            ignoreCase: true);
        Assert.NotEmpty(contentRights.AssertProperty("rights").EnumerateArray());
    }

    private async Task<JsonElement> GetSensitivityLabelsAsync(string[] labelIds)
    {
        var result = await CallToolAsync(
            "purview_sensitivitylabel_get",
            new()
            {
                { "tenant", GetTenantId() },
                { "user-email", GetFixtureValue(TestUserEmailOutput, PlaybackTestUserEmail) },
                { "label-ids", labelIds },
                { "content-target", "File" }
            });

        Assert.NotNull(result);
        return result.Value;
    }

    private string[] GetLabelIds() =>
    [
        GetFixtureValue(LowPriorityLabelIdOutput, PlaybackLowPriorityLabelId),
        GetFixtureValue(HighPriorityLabelIdOutput, PlaybackHighPriorityLabelId)
    ];

    private string GetTenantId() => TestMode == Microsoft.Mcp.Tests.Helpers.TestMode.Playback ? PlaybackTenantId : Settings.TenantId;

    private string GetFixtureValue(string outputName, string playbackValue)
    {
        if (TestMode == Microsoft.Mcp.Tests.Helpers.TestMode.Playback)
        {
            return playbackValue;
        }

        if (Settings.DeploymentOutputs.TryGetValue(outputName, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        Assert.Skip($"The {outputName} deployment output is required for Purview live tests.");
        return string.Empty;
    }

    private void AddDeploymentOutputSanitizer(string outputName, string replacement)
    {
        if (Settings.DeploymentOutputs.TryGetValue(outputName, out var value))
        {
            AddSanitizer(value, replacement);
        }
    }

    private void AddSanitizer(string value, string replacement)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        GeneralRegexSanitizers.Add(new(new()
        {
            Regex = Regex.Escape(value),
            Value = replacement
        }));
    }

    private static IEnumerable<string> EnumerateLabelIds(JsonElement labels)
    {
        foreach (var label in labels.EnumerateArray())
        {
            yield return label.AssertProperty("id").GetString()!;

            if (label.TryGetProperty("sublabels", out var sublabels))
            {
                foreach (var id in EnumerateLabelIds(sublabels))
                {
                    yield return id;
                }
            }
        }
    }
}