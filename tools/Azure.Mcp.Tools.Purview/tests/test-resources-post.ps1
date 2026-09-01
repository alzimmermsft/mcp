param(
    [string] $TenantId,
    [string] $TestApplicationId,
    [string] $ResourceGroupName,
    [string] $BaseName,
    [hashtable] $DeploymentOutputs,
    [hashtable] $AdditionalParameters
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

. "$PSScriptRoot/../../../eng/common/scripts/common.ps1"
. "$PSScriptRoot/../../../eng/scripts/helpers/TestResourcesHelpers.ps1"

# Purview labels, label policies, DLP policies, licenses, and Graph admin consent are
# long-lived Microsoft 365 tenant prerequisites and cannot be provisioned through ARM.
$fixtureParameters = [ordered]@{
    "PURVIEW_TEST_USER_ID"                = "PurviewTestUserId"
    "PURVIEW_TEST_USER_EMAIL"             = "PurviewTestUserEmail"
    "PURVIEW_TEST_LOW_PRIORITY_LABEL_ID"  = "PurviewTestLowPriorityLabelId"
    "PURVIEW_TEST_HIGH_PRIORITY_LABEL_ID" = "PurviewTestHighPriorityLabelId"
}

foreach ($fixtureParameter in $fixtureParameters.GetEnumerator()) {
    $outputName = $fixtureParameter.Key
    if (-not [string]::IsNullOrWhiteSpace($DeploymentOutputs[$outputName])) {
        continue
    }

    $parameterName = $fixtureParameter.Value
    $armOutputName = $parameterName.ToUpperInvariant()
    $value = if (-not [string]::IsNullOrWhiteSpace($DeploymentOutputs[$armOutputName])) {
        $DeploymentOutputs[$armOutputName]
    }
    elseif ($AdditionalParameters -and $AdditionalParameters.ContainsKey($parameterName)) {
        $AdditionalParameters[$parameterName]
    }
    else {
        [Environment]::GetEnvironmentVariable($outputName)
    }

    if (-not [string]::IsNullOrWhiteSpace($value)) {
        $DeploymentOutputs[$outputName] = $value
    }
}

$guidOutputs = @(
    "PURVIEW_TEST_USER_ID",
    "PURVIEW_TEST_LOW_PRIORITY_LABEL_ID",
    "PURVIEW_TEST_HIGH_PRIORITY_LABEL_ID"
)
foreach ($outputName in $guidOutputs) {
    $value = $DeploymentOutputs[$outputName]
    $parsedGuid = [guid]::Empty
    if (-not [string]::IsNullOrWhiteSpace($value) -and -not [guid]::TryParse($value, [ref]$parsedGuid)) {
        throw "$outputName must be a valid GUID."
    }
}

$userEmail = $DeploymentOutputs["PURVIEW_TEST_USER_EMAIL"]
if (-not [string]::IsNullOrWhiteSpace($userEmail)) {
    try {
        $parsedEmail = [System.Net.Mail.MailAddress]::new($userEmail)
    }
    catch {
        throw "PURVIEW_TEST_USER_EMAIL must be a valid email address."
    }

    if ($parsedEmail.Address -ne $userEmail) {
        throw "PURVIEW_TEST_USER_EMAIL must be a valid email address."
    }
}

$lowPriorityLabelId = $DeploymentOutputs["PURVIEW_TEST_LOW_PRIORITY_LABEL_ID"]
$highPriorityLabelId = $DeploymentOutputs["PURVIEW_TEST_HIGH_PRIORITY_LABEL_ID"]
if (-not [string]::IsNullOrWhiteSpace($lowPriorityLabelId) -and $lowPriorityLabelId -eq $highPriorityLabelId) {
    throw "The low-priority and high-priority Purview test labels must be different."
}

$missingFixtureValues = $fixtureParameters.Keys | Where-Object {
    [string]::IsNullOrWhiteSpace($DeploymentOutputs[$_])
}
if ($missingFixtureValues.Count -gt 0) {
    Write-Warning "Purview tenant fixture values are missing: $($missingFixtureValues -join ', '). Dependent live tests will be skipped."
}

$testSettings = New-TestSettings @PSBoundParameters -OutputPath $PSScriptRoot