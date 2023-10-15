namespace ZipUtils;

/// <summary>
/// Contains information about extracted file.
/// </summary>
/// <param name="Name">File name.</param>
/// <param name="Size">File size in bytes.</param>
public readonly record struct ExtractedFileInfo(string Name, long Size);
