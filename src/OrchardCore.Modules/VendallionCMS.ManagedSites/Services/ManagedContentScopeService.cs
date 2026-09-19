using OrchardCore.ContentManagement;
using VendallionCMS.ManagedSites.Models;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Answers the two scope questions a Managed Content item carries.
/// </summary>
public interface IManagedContentScopeService
{
    /// <summary>
    /// Determines whether a Managed Site may override an item.
    /// </summary>
    /// <param name="part">The Managed Content part, or <see langword="null" /> when the item carries none.</param>
    /// <param name="managedSiteId">The Managed Site identifier.</param>
    /// <returns><see langword="true" /> when the Managed Site may override the item.</returns>
    bool CanEdit(ManagedContentPart part, string managedSiteId);

    /// <summary>
    /// Determines whether an item renders for a request context.
    /// </summary>
    /// <param name="part">The Managed Content part, or <see langword="null" /> when the item carries none.</param>
    /// <param name="managedSiteId">The resolved Managed Site, or <see langword="null" /> for the Site Blueprint context.</param>
    /// <returns><see langword="true" /> when the item renders.</returns>
    bool CanDisplay(ManagedContentPart part, string managedSiteId);

    /// <summary>
    /// Determines whether a content item renders for a request context.
    /// </summary>
    /// <param name="contentItem">The content item.</param>
    /// <param name="managedSiteId">The resolved Managed Site, or <see langword="null" /> for the Site Blueprint context.</param>
    /// <returns><see langword="true" /> when the item renders.</returns>
    bool CanDisplay(ContentItem contentItem, string managedSiteId);
}

/// <summary>
/// Evaluates Managed Content edit and display scopes.
/// </summary>
/// <remarks>
/// A content item without the part is never restricted, which is what keeps the capability opt-in: a
/// type that has not been given the part behaves exactly as it did before the feature existed.
/// </remarks>
public sealed class ManagedContentScopeService : IManagedContentScopeService
{
    /// <inheritdoc />
    public bool CanEdit(ManagedContentPart part, string managedSiteId)
    {
        // Only a Managed Site can hold an override, so the Site Blueprint context never "edits" here.
        // Blueprint administrators change the original item through the standard admin UI instead.
        if (part is null || string.IsNullOrEmpty(managedSiteId))
        {
            return false;
        }

        return part.EditScope?.Includes(managedSiteId) == true;
    }

    /// <inheritdoc />
    public bool CanDisplay(ManagedContentPart part, string managedSiteId)
    {
        if (part is null)
        {
            return true;
        }

        if (string.IsNullOrEmpty(managedSiteId))
        {
            return part.DisplayInBlueprintContext;
        }

        return part.DisplayScope?.Includes(managedSiteId) == true;
    }

    /// <inheritdoc />
    public bool CanDisplay(ContentItem contentItem, string managedSiteId)
        => CanDisplay(contentItem?.As<ManagedContentPart>(), managedSiteId);
}
