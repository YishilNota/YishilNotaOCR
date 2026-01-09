using System.IO;
using System.IO.Compression;

namespace YishilNotaOCR.Utils
{
    public static class EpubGenerator
    {
        public static void CreateEpub(string outputPath, string title, string content)
        {
            using (FileStream fs = new FileStream(outputPath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                // 1. mimetype (必须是第一个文件，且不能压缩)
                var mimetypeEntry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
                using (var writer = new StreamWriter(mimetypeEntry.Open())) writer.Write("application/epub+zip");

                // 2. META-INF/container.xml
                var containerEntry = archive.CreateEntry("META-INF/container.xml");
                using (var writer = new StreamWriter(containerEntry.Open()))
                {
                    writer.Write("<?xml version=\"1.0\"?><container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\"><rootfiles><rootfile full-path=\"OEBPS/content.opf\" media-type=\"application/oebps-package+xml\"/></rootfiles></container>");
                }

                // 3. OEBPS/content.opf (配置 RTL 方向)
                var opfEntry = archive.CreateEntry("OEBPS/content.opf");
                using (var writer = new StreamWriter(opfEntry.Open()))
                {
                    writer.Write($@"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" unique-identifier=""bookid"" version=""2.0"">
    <metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"">
        <dc:title>{title}</dc:title>
        <dc:language>ug</dc:language>
    </metadata>
    <manifest>
        <item id=""ncx"" href=""toc.ncx"" media-type=""application/x-dtbncx+xml""/>
        <item id=""content"" href=""text.xhtml"" media-type=""application/xhtml+xml""/>
    </manifest>
    <spine toc=""ncx"">
        <itemref idref=""content""/>
    </spine>
</package>");
                }

                // 4. OEBPS/toc.ncx (简易目录)
                var ncxEntry = archive.CreateEntry("OEBPS/toc.ncx");
                using (var writer = new StreamWriter(ncxEntry.Open()))
                {
                    writer.Write($@"<?xml version=""1.0"" encoding=""UTF-8""?>
<ncx xmlns=""http://www.daisy.org/z3986/2005/ncx/"" version=""2005-1"">
    <docTitle><text>{title}</text></docTitle>
    <navMap><navPoint id=""navpoint-1"" playOrder=""1""><navLabel><text>Content</text></navLabel><content src=""text.xhtml""/></navPoint></navMap>
</ncx>");
                }

                // 5. OEBPS/text.xhtml (内容主体：处理换行并设置 RTL)
                var xhtmlEntry = archive.CreateEntry("OEBPS/text.xhtml");
                using (var writer = new StreamWriter(xhtmlEntry.Open()))
                {
                    // 处理 TXT 换行符
                    string htmlContent = content.Replace("\r\n", "<br/>").Replace("\n", "<br/>");
                    writer.Write($@"<?xml version=""1.0"" encoding=""utf-8""?>
<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.1//EN"" ""http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"" dir=""rtl"" xml:lang=""ug"">
<head><title>{title}</title>
<style>body {{ font-family: 'Microsoft Uighur', serif; padding: 5%; direction: rtl; }}</style>
</head>
<body><div>{htmlContent}</div></body>
</html>");
                }
            }
        }
    }
}
