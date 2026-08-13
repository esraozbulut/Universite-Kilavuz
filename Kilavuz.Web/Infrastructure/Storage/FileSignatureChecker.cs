using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Kilavuz.Web.Infrastructure.Storage;

public static class FileSignatureChecker
{
    private static readonly Dictionary<string, List<byte[]>> FileSignatures = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".jpeg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".jpg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
        { ".gif", new List<byte[]> { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
        { ".webp", new List<byte[]> { new byte[] { 0x52, 0x49, 0x46, 0x46 } } },
        { ".pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } } },
        { ".zip", new List<byte[]> 
            {
                new byte[] { 0x50, 0x4B, 0x03, 0x04 }, 
                new byte[] { 0x50, 0x4B, 0x4C, 0x49, 0x54, 0x45 },
                new byte[] { 0x50, 0x4B, 0x53, 0x70, 0x58 },
                new byte[] { 0x50, 0x4B, 0x05, 0x06 },
                new byte[] { 0x50, 0x4B, 0x07, 0x08 },
                new byte[] { 0x57, 0x69, 0x6E, 0x5A, 0x69, 0x70 }
            }
        },
        { ".docx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } }, // docx is actually a zip
        { ".xlsx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } }, // xlsx is actually a zip
        { ".pptx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } }  // pptx is actually a zip
    };

    public static bool IsValidSignature(IFormFile file, string extension)
    {
        if (string.IsNullOrEmpty(extension)) return false;

        // Bazi metin tabanli dosyalarin (CSV vb.) magic byte'i yoktur, onlari bypass ediyoruz
        if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!FileSignatures.ContainsKey(extension))
        {
            return false;
        }

        using var stream = file.OpenReadStream();
        using var reader = new BinaryReader(stream, System.Text.Encoding.Default, leaveOpen: true);
        var signatures = FileSignatures[extension];
        var headerBytes = reader.ReadBytes(signatures.Max(m => m.Length));
        
        stream.Position = 0; // Reset stream position

        return signatures.Any(signature => 
            headerBytes.Take(signature.Length).SequenceEqual(signature));
    }
}
