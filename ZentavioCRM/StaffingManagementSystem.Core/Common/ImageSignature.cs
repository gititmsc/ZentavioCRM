namespace ZentavioCRM.Core.Common
{
    /// <summary>
    /// Detects an image's real format from its file signature ("magic bytes") rather than trusting a
    /// client-supplied Content-Type header. A browser sets that header from the file's EXTENSION, not
    /// its actual bytes — renaming a non-image file (e.g. an HTML file) to ".png" is enough to make it
    /// arrive as "image/png". Any endpoint accepting an image upload should validate against this,
    /// not against <c>IFormFile.ContentType</c> alone.
    /// </summary>
    public static class ImageSignature
    {
        /// <summary>Attempts to detect PNG, JPEG, or GIF from the leading bytes of <paramref name="content"/>.
        /// Returns the canonical MIME type on success, or null if the bytes don't match any accepted signature.</summary>
        public static string? Detect(byte[]? content)
        {
            if (content is null || content.Length < 8)
            {
                return null;
            }

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47 &&
                content[4] == 0x0D && content[5] == 0x0A && content[6] == 0x1A && content[7] == 0x0A)
            {
                return "image/png";
            }

            // JPEG: FF D8 FF
            if (content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
            {
                return "image/jpeg";
            }

            // GIF: ASCII "GIF87a" or "GIF89a"
            if (content[0] == 0x47 && content[1] == 0x49 && content[2] == 0x46 && content[3] == 0x38 &&
                (content[4] == 0x37 || content[4] == 0x39) && content[5] == 0x61)
            {
                return "image/gif";
            }

            return null;
        }
    }
}
