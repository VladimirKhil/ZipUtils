# ZipUtils

Provides helper methods for working with ZIP files.

## Usage

You could use `ZipExtractor` class to safely extract ZIP archive. This class allows you to:

- limit maximum unzipped data size (protecting you from ZIP bombs)
- filter files to be extracted
- rename extracted files and use safe (not user-provided) names

To extract an archive, call `ExtractArchiveFileToFolderAsync` method and provide corresponding options to it:

- `MaxAllowedDataLength` limits extracted data size
- `FileFilter` allows you to exclude some files from extraction
- `FileNamingModeSelector` allows to select file renaming mode globally or for each file individually

# NuGet package usage

```dotnet add package VKhil.ZipUtils```