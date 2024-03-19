namespace ZipUtils.Test;

public sealed class ZipExtractorTests
{
    [Test]
    public async Task ExtractArchiveFileToFolderAsync_Ok()
    {
        var targetFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var extractedFiles = await ZipExtractor.ExtractArchiveFileToFolderAsync("test.zip", targetFolder);

            Assert.That(extractedFiles, Has.Count.EqualTo(8));

            Assert.That(extractedFiles, Contains.Item(new KeyValuePair<string, ExtractedFileInfo>(
                "content.xml",
                new ExtractedFileInfo("content.xml", 11584))));
        }
        finally
        {
            Directory.Delete(targetFolder, true);
        }
    }

    [Test]
    public async Task ExtractArchiveFileToFolderAsync_WithFilterAndSelector_Ok()
    {
        var targetFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        static bool fileFilter(string filePath)
        {
            if (filePath == "content.xml")
            {
                return true;
            }

            var folderName = Path.GetDirectoryName(filePath);

            return folderName switch
            {
                "Images" or "Audio" or "Video" or "Texts" => true,
                _ => false
            };
        }

        try
        {
            var extractedFiles = await ZipExtractor.ExtractArchiveFileToFolderAsync(
                "test.zip",
                targetFolder,
                new ExtractionOptions(name => name switch
                {
                    "content.xml" or "Texts/authors.xml" or "Texts/sources.xml" => UnzipNamingMode.KeepOriginal,
                    _ => UnzipNamingMode.Hash
                })
                {
                    FileFilter = fileFilter
                });

            Assert.That(extractedFiles, Has.Count.EqualTo(7));
            Assert.That(extractedFiles.Values, Contains.Item(new ExtractedFileInfo("content.xml", 11584)));
            Assert.That(extractedFiles.Values, Contains.Item(new ExtractedFileInfo(Path.Combine("Images", "549C3F433C5FBA8C.png"), 31924)));
        }
        finally
        {
            Directory.Delete(targetFolder, true);
        }
    }

    [Test]
    public void ExtractArchiveFileToFolderAsync_BadPath_Fail()
    {
        var targetFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            Assert.ThrowsAsync<InvalidOperationException>(() => ZipExtractor.ExtractArchiveFileToFolderAsync("badFolder.zip", targetFolder));
        }
        finally
        {
            Directory.Delete(targetFolder, true);
        }
    }
}