using System;
using System.Collections.Generic;
using System.IO;
using PdfSharp.Fonts;

namespace SistemPerpustakaan.Pembantu
{
    // Simple font resolver for MigraDoc/PDFsharp on Windows.
    // Maps common font family names to files in C:\Windows\Fonts.
    public class FontResolver : IFontResolver
    {
        private readonly string _fontsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
        private readonly Dictionary<string, string> _map = new()
        {
            // key: family name (lower)
            { "arial", "arial" },
            { "courier new", "cour" },
            { "times new roman", "times" },
            { "segoe ui", "segoeui" },
            { "calibri", "calibri" }
        };

        public byte[] GetFont(string faceName)
        {
            // faceName is like "arial#Regular" or "cour#Bold" depending on ResolveTypeface
            if (string.IsNullOrEmpty(faceName)) return null;
            var parts = faceName.Split('#');
            var key = parts[0].ToLowerInvariant();
            var style = parts.Length > 1 ? parts[1] : "regular";

            // Determine filename based on key and style
            string fileName = GetFileNameForKeyStyle(key, style);
            if (fileName == null)
                return null;

            var path = Path.Combine(_fontsFolder, fileName);
            if (!File.Exists(path))
            {
                // try .ttf or .otf variations
                var alt = Path.Combine(_fontsFolder, fileName + ".ttf");
                if (File.Exists(alt)) path = alt;
                else
                {
                    alt = Path.Combine(_fontsFolder, fileName + ".otf");
                    if (File.Exists(alt)) path = alt;
                    else
                    {
                        return null;
                    }
                }
            }

            return File.ReadAllBytes(path);
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            var fam = (familyName ?? string.Empty).ToLowerInvariant();
            // Try to map family to our key
            string key = null;
            foreach (var kv in _map)
            {
                if (fam.Contains(kv.Key))
                {
                    key = kv.Value;
                    break;
                }
            }

            if (key == null)
            {
                // fallback to arial
                key = "arial";
            }

            string style;
            if (isBold && isItalic) style = "BoldItalic";
            else if (isBold) style = "Bold";
            else if (isItalic) style = "Italic";
            else style = "Regular";

            // return face name: key#style
            return new FontResolverInfo(key + "#" + style);
        }

        private string GetFileNameForKeyStyle(string key, string style)
        {
            // Normalize key: if it's like "arial" or "cour"
            // Map based on known patterns. Use Windows font file names.
            key = key.ToLowerInvariant();
            style = style.ToLowerInvariant();

            if (key.StartsWith("arial"))
            {
                return style switch
                {
                    "bolditalic" => "arialbi.ttf",
                    "bold" => "arialbd.ttf",
                    "italic" => "ariali.ttf",
                    _ => "arial.ttf",
                };
            }

            if (key.StartsWith("cour") || key.Contains("courier"))
            {
                return style switch
                {
                    "bolditalic" => "courbi.ttf",
                    "bold" => "courbd.ttf",
                    "italic" => "couri.ttf",
                    _ => "cour.ttf",
                };
            }

            if (key.StartsWith("times"))
            {
                return style switch
                {
                    "bolditalic" => "timesbi.ttf",
                    "bold" => "timesbd.ttf",
                    "italic" => "timesi.ttf",
                    _ => "times.ttf",
                };
            }

            if (key.StartsWith("segoeui") || key.Contains("segoe"))
            {
                return style switch
                {
                    "bolditalic" => "segoeui.ttf", // no separate bolditalic file usually
                    "bold" => "segoeuib.ttf",
                    "italic" => "segoeuii.ttf",
                    _ => "segoeui.ttf",
                };
            }

            if (key.StartsWith("calibri"))
            {
                return style switch
                {
                    "bolditalic" => "calibriz.ttf", // may not exist
                    "bold" => "calibrib.ttf",
                    "italic" => "calibrii.ttf",
                    _ => "calibri.ttf",
                };
            }

            return null;
        }
    }
}
