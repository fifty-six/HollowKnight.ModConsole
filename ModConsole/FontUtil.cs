using UnityEngine;

namespace ModConsole
{
    public static class FontUtil
    {
        private static readonly string[] OSFonts =
        {
            // Windows
            "Consolas",
            // Mac
            "Menlo",
            // Linux
            "Courier New",
            "DejaVu Mono"
        };

        public static Font Font { get; } = FindFont();

        private static Font FindFont()
        {
            Font font = null;
            
            foreach (string fontName in OSFonts)
            {
                font = Font.CreateDynamicFontFromOSFont(fontName, 12);

                // Found a monospace OS font.
                if (font != null)
                    break;
            }

            // Fallback
            font ??= Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;

            return font;
        }
    }
}