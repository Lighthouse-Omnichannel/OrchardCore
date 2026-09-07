using VendallionCMS.ManagedSites.Models;
using Xunit;

namespace VendallionCMS.ManagedSites.Tests.Routing;

public class ManagedSiteDefinitionTests
{
    [Fact]
    public void ManagedSite_DefaultStatus_IsDraft()
    {
        var managedSite = new ManagedSite();

        Assert.Equal(ManagedSiteStatus.Draft, managedSite.Status);
    }
}
