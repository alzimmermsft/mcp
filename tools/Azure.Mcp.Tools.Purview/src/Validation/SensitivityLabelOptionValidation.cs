// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Net.Mail;
using Microsoft.Mcp.Core.Commands;

namespace Azure.Mcp.Tools.Purview.Validation;

internal static class SensitivityLabelOptionValidation
{
    public static void ValidateUserEmail(string userEmail, ValidationResult validationResult)
    {
        if (userEmail.Length > 320 || !MailAddress.TryCreate(userEmail, out var address) ||
            !address.Address.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
        {
            validationResult.Errors.Add("--user-email must be a valid email address.");
        }
    }

    public static void ValidateLabelIds(IEnumerable<string>? labelIds, ValidationResult validationResult)
    {
        if (labelIds?.Any(static labelId => !Guid.TryParse(labelId, out _)) == true)
        {
            validationResult.Errors.Add("--label-ids must contain valid sensitivity label IDs (GUIDs).");
        }
    }

    public static void ValidateLocale(string locale, ValidationResult validationResult)
    {
        if (locale.Length > 35)
        {
            validationResult.Errors.Add("--locale must be a valid locale name of 35 characters or fewer.");
            return;
        }

        try
        {
            _ = CultureInfo.GetCultureInfo(locale);
        }
        catch (CultureNotFoundException)
        {
            validationResult.Errors.Add("--locale must be a valid locale name of 35 characters or fewer.");
        }
    }

    public static void ValidateContentFormat(string contentFormat, ValidationResult validationResult)
    {
        if (contentFormat.Length > 100)
        {
            validationResult.Errors.Add("--content-format must be 100 characters or fewer.");
        }
    }

    public static void ValidateContentFormats(IEnumerable<string>? contentFormats, ValidationResult validationResult)
    {
        if (contentFormats?.Any(static contentFormat => contentFormat.Length > 100) == true)
        {
            validationResult.Errors.Add("--content-formats must contain values of 100 characters or fewer.");
        }
    }

    public static void ValidateContentId(string contentId, ValidationResult validationResult)
    {
        if (contentId.Length > 1024)
        {
            validationResult.Errors.Add("--content-id must be 1,024 characters or fewer.");
        }
    }
}
