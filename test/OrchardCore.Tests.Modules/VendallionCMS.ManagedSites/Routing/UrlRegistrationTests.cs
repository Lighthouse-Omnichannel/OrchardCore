using System;
using System.Collections.Generic;
using VendallionCMS.ManagedSites.Models;
using VendallionCMS.ManagedSites.Services;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Routing;

public class UrlRegistrationTests
{
    [Fact]
    public void ValidateNoConflict_DuplicateActiveUrl_Throws()
    {
        var registration = new UrlRegistration
        {
            Id = "two",
            Url = "Example",
            Status = UrlRegistrationStatus.Active,
        };
        var existing = new List<UrlRegistration>
        {
            new()
            {
                Id = "one",
                Url = "/example/",
                Status = UrlRegistrationStatus.Active,
            },
        };

        Assert.Throws<InvalidOperationException>(() => UrlRegistrationValidator.ValidateNoConflict(registration, existing));
    }

    [Fact]
    public void Normalize_AddsLeadingSlashAndTrimsTrailingSlash()
    {
        var normalized = UrlRegistrationValidator.Normalize("Example/");

        Assert.Equal("/example", normalized);
    }
}
