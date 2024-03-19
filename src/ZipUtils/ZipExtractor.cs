using System.IO.Compression;
using System.Runtime.InteropServices;

namespace ZipUtils;

/// <summary>
/// Provides helper methods for extracting zip archives.
/// </summary>
public static class ZipExtractor
{
    /// <summary>
    /// Defines a maxumim file length on current platform.
    /// </summary>
    internal static int MaxFileNameLength =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 100 : 255 / 2; // / 2 because of 2-byte non-ASCII symbols

    /// <summary>
    /// Etracts zip archive to folder.
    /// </summary>
    /// <param name="sourceArchiveFilePath">Arhive file path.</param>
    /// <param name="destinationFolderPath">Target folder path.</param>
    /// <param name="extractionOptions">Extraction options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Map of archive file names to extracted file info.</returns>
    /// <exception cref="InvalidOperationException" />
    /// <remarks>
    /// File names are forcibly hashed when they are too long.
    /// </remarks>
    public static async Task<IReadOnlyDictionary<string, ExtractedFileInfo>> ExtractArchiveFileToFolderAsync(
        string sourceArchiveFilePath,
        string destinationFolderPath,
        ExtractionOptions? extractionOptions = null,
        CancellationToken cancellationToken = default)
    {
        extractionOptions ??= ExtractionOptions.Default;

        Directory.CreateDirectory(destinationFolderPath);

        using var stream = File.OpenRead(sourceArchiveFilePath);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        // Quickly check the value from the zip header
        var declaredSize = archive.Entries.Sum(entry => entry.Length);

        if (declaredSize > extractionOptions.MaxAllowedDataLength)
        {
            throw new InvalidOperationException($"Archive data is too big ({declaredSize} bytes)");
        }

        var extractedFiles = new Dictionary<string, ExtractedFileInfo>();

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (extractionOptions.FileFilter != null && !extractionOptions.FileFilter(entry.FullName))
            {
                continue;
            }

            var namingMode = extractionOptions.FileNamingModeSelector(entry.FullName);

            var targetFileInfo = await ExtractEntryToFolderAsync(entry, destinationFolderPath, namingMode, cancellationToken);

            if (targetFileInfo.HasValue)
            {
                extractedFiles[entry.FullName] = targetFileInfo.Value;
            }
        }

        return extractedFiles;
    }

    /// <summary>
    /// Extracts entry to folder.
    /// </summary>
    /// <param name="entry">Entry to extract.</param>
    /// <param name="destinationFolderPath">Target folder path.</param>
    /// <param name="fileNamingMode">Extracted file naming mode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Extracted file relative path.</returns>
    /// <exception cref="InvalidOperationException">Target file name is too long.</exception>
    private static async Task<ExtractedFileInfo?> ExtractEntryToFolderAsync(
        ZipArchiveEntry entry,
        string destinationFolderPath,
        UnzipNamingMode fileNamingMode = UnzipNamingMode.KeepOriginal,
        CancellationToken cancellationToken = default)
    {
        var targetSubfolderPath = PrepareFolderForExtraction(entry, destinationFolderPath);

        var targetFileName = fileNamingMode == UnzipNamingMode.Unescape ? Uri.UnescapeDataString(entry.Name) : entry.Name;

        if (fileNamingMode == UnzipNamingMode.Hash || targetFileName.Length > MaxFileNameLength)
        {
            targetFileName = HashHelper.CalculateHash(targetFileName);
        }

        var relativeTargetFileName = Path.Combine(targetSubfolderPath, targetFileName);
        var targetPath = Path.Combine(destinationFolderPath, relativeTargetFileName);

        if (Path.GetFileName(targetPath).Length > MaxFileNameLength)
        {
            throw new InvalidOperationException(
                $"Too long target file name: \"{targetPath}\", entry.FullName: \"{entry.FullName}\". " +
                $"Maximum allowed length: {MaxFileNameLength}");
        }

        if (!entry.FullName.EndsWith(Path.AltDirectorySeparatorChar)) // Not a directory
        {
            await ZipFileExtensionsPatched.ExtractToFileAsync(entry, targetPath, true, cancellationToken);
            return new ExtractedFileInfo(relativeTargetFileName, new FileInfo(targetPath).Length);
        }

        return null;
    }

    /// <summary>
    /// Creates folder for entry extraction and returns target file path.
    /// </summary>
    /// <param name="entry">Entry to extract.</param>
    /// <param name="destinationFolderPath">Target folder path.</param>
    /// <returns>Target subfolder path.</returns>
    private static string PrepareFolderForExtraction(ZipArchiveEntry entry, string destinationFolderPath)
    {
        // TODO: support nested folders
        var directorySeparatorIndex = entry.FullName.IndexOf(Path.AltDirectorySeparatorChar);

        if (directorySeparatorIndex == -1)
        {
            return "";
        }

        var subFolderPath = entry.FullName[..directorySeparatorIndex];

        // TODO: support safe renaming for folder names

        var targetDir = Path.Combine(destinationFolderPath, subFolderPath);
        ValidateEntryFullPath(entry.FullName, destinationFolderPath, targetDir);

        Directory.CreateDirectory(targetDir);

        return subFolderPath;
    }

    private static void ValidateEntryFullPath(string entryFullPath, string destinationFolderPath, string targetDir)
    {
        var targetDirFullPath = Path.GetFullPath(targetDir);
        var descinationFolderFullPath = Path.GetFullPath(destinationFolderPath + Path.DirectorySeparatorChar);

        if (!targetDirFullPath.StartsWith(descinationFolderFullPath))
        {
            throw new InvalidOperationException($"Entry {entryFullPath} is outside target directory");
        }
    }
}
