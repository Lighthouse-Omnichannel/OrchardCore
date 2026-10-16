using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Media;

namespace VendallionCMS.ManagedSites.Services;

/// <summary>
/// Gives a Managed Site the media folder its own assets live in.
/// </summary>
/// <remarks>
/// The folder is at the root of the media library and named by the Managed Site code, because the
/// platform resolves a folder's permission from the first segment of a path. A folder nested under a
/// shared parent would be governed by that parent's permission and could not be told apart from its
/// siblings, which is the whole point of giving a Managed Site one.
/// </remarks>
public interface IManagedSiteMediaFolderService
{
    /// <summary>
    /// Gets the media folder a Managed Site's own assets live in.
    /// </summary>
    /// <param name="managedSiteCode">The Managed Site code.</param>
    /// <returns>The folder path, which is the code.</returns>
    string GetFolder(string managedSiteCode);

    /// <summary>
    /// Creates the Managed Site's media folder if it is not there already.
    /// </summary>
    /// <remarks>
    /// Done when the Managed Site is defined rather than on first upload: a permission exists only for
    /// a folder that exists, so a folder made on first upload could not be granted to anybody before
    /// that upload, and the editor who needed it would be the one person unable to make it.
    /// </remarks>
    /// <param name="managedSiteCode">The Managed Site code.</param>
    /// <returns>Whether a folder was created.</returns>
    ValueTask<bool> EnsureAsync(string managedSiteCode);

    /// <summary>
    /// Reads the Managed Site a media path belongs to, if any.
    /// </summary>
    /// <param name="path">The media path.</param>
    /// <returns>The first segment of the path, or <see langword="null" /> when there is none.</returns>
    string GetOwningFolder(string path);
}

/// <summary>
/// Creates and names a Managed Site's media folder, where the Media feature is enabled to hold one.
/// </summary>
/// <remarks>
/// The file store is resolved when it is wanted rather than injected, so that defining a Managed Site
/// works on a tenant that does not run the Media feature. There, a Managed Site simply has nowhere to
/// put assets, which is not a reason to refuse to define it.
/// </remarks>
public sealed class ManagedSiteMediaFolderService : IManagedSiteMediaFolderService
{
    private static readonly char[] s_separators = ['/', '\\'];

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedSiteMediaFolderService" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider the media file store is read from.</param>
    public ManagedSiteMediaFolderService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public string GetFolder(string managedSiteCode)
        => string.IsNullOrWhiteSpace(managedSiteCode) ? null : managedSiteCode.Trim();

    /// <inheritdoc />
    public async ValueTask<bool> EnsureAsync(string managedSiteCode)
    {
        var folder = GetFolder(managedSiteCode);

        if (folder is null)
        {
            return false;
        }

        var fileStore = _serviceProvider.GetService<IMediaFileStore>();

        if (fileStore is null)
        {
            return false;
        }

        if (await fileStore.GetDirectoryInfoAsync(folder) is not null)
        {
            return false;
        }

        return await fileStore.TryCreateDirectoryAsync(folder);
    }

    /// <inheritdoc />
    public string GetOwningFolder(string path)
        => GetOwningFolderOf(path);

    /// <summary>
    /// Reads the root folder a media path sits in.
    /// </summary>
    /// <remarks>
    /// Separated from the service so the rule can be tested without a file store behind it. Both
    /// separators are treated alike because a path reaches authorization as it was written rather than
    /// as the store would normalize it.
    /// </remarks>
    /// <param name="path">The media path.</param>
    /// <returns>The first segment, or <see langword="null" /> when the path names no folder.</returns>
    internal static string GetOwningFolderOf(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var trimmed = path.Trim().Trim(s_separators);

        if (trimmed.Length == 0)
        {
            return null;
        }

        var separator = trimmed.IndexOfAny(s_separators);

        return separator < 0 ? trimmed : trimmed[..separator];
    }
}
