using OrchardCore.Entities;
using OrchardCore.Users.Models;
using VendallionCMS.ManagedSites.Models;
using YesSql.Indexes;

namespace VendallionCMS.ManagedSites.Indexes;

/// <summary>
/// Indexes one row per Managed Site a user holds clearance for.
/// </summary>
/// <remarks>
/// Clearance is stored on the user, because that is what it is: a grant to a person, read from the user
/// when their principal is built. Administering it is the other way round -- who may work on this
/// Managed Site -- and that question cannot be asked of the users without reading every one of them.
/// One row per pairing makes it a single indexed lookup instead.
///
/// The row says only that a grant exists. What the grant allows is read from the user, and whether it
/// is currently effective is decided against the Managed Site definitions, so revoking or disabling a
/// Managed Site takes effect without rewriting anything here.
/// </remarks>
public sealed class ManagedSiteClearanceIndex : MapIndex
{
    /// <summary>
    /// Gets or sets the Managed Site the grant covers.
    /// </summary>
    public string ManagedSiteCode { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user holding it.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Gets or sets the user's normalized user name, so that the administration screen can order and
    /// narrow by it without loading every user it lists.
    /// </summary>
    public string NormalizedUserName { get; set; }
}

/// <summary>
/// Maps the Managed Site grants held by each user.
/// </summary>
public sealed class ManagedSiteClearanceIndexProvider : IndexProvider<User>
{
    /// <inheritdoc />
    public override void Describe(DescribeContext<User> context)
    {
        context.For<ManagedSiteClearanceIndex, ManagedSiteClearanceIndex>()
            .Map(user =>
            {
                if (!user.TryGet<ManagedSiteClearanceSettings>(out var settings))
                {
                    return [];
                }

                return settings.Grants
                    .Where(grant => !string.IsNullOrEmpty(grant.ManagedSiteCode))
                    .Select(grant => grant.ManagedSiteCode)
                    .Distinct(StringComparer.Ordinal)
                    .Select(managedSiteCode => new ManagedSiteClearanceIndex
                    {
                        ManagedSiteCode = managedSiteCode,
                        UserId = user.UserId,
                        NormalizedUserName = user.NormalizedUserName,
                    });
            });
    }
}
