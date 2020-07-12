using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Modding;
using UnityEngine;
using UnityEngine.UI;

namespace ModConsole
{
    public static class FontUtil
    {
        public static Font LoadFont(string request = null)
        {
            Assembly asm = Assembly.GetExecutingAssembly();

            // If Fira Code fails to load for whatever reason, Arial is a good backup
            var font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;

            if (!string.IsNullOrEmpty(request))
            {
                font = Font.CreateDynamicFontFromOSFont(request, 1);

                if (font.name == "Arial")
                    ModConsole.Instance.LogWarn($"Unable to find font {request}, falling back to Fira Code.");
                else
                    return font;
            }

            foreach (string res in asm.GetManifestResourceNames())
            {
                using Stream s = asm.GetManifestResourceStream(res);

                AssetBundle ab = AssetBundle.LoadFromStream(s);

                var firaCode = ab.LoadAsset<Font>("FiraCode-Regular.ttf");

                if (firaCode != null)
                    font = firaCode;
            }

            return font;
        }

        public static IEnumerator ChangeAPIFont(Font font)
        {
            if (ReflectionHelper.GetAttr<ModHooks, ModHooksGlobalSettings>(ModHooks.Instance, "_globalSettings").ShowDebugLogInGame)
            {
                GameObject console;

                while ((console = GameObject.Find("ModdingApiConsoleLog")) == null)
                    yield return null;

                console.GetComponentInChildren<Text>(true).font = font;
            }

            // Hide the failed to load error for Mono.CSharp because it actually works anyways and confusing people is annoying.
            Type modLoader = Type.GetType("Modding.ModLoader, Assembly-CSharp");

            FieldInfo fi = modLoader?.GetField("_draw", BindingFlags.Static | BindingFlags.NonPublic);

            if (fi == null) yield break;

            ModVersionDraw draw;

            // Have to wait for it to show up because of preloading
            while ((draw = (ModVersionDraw) fi.GetValue(null)) == null)
            {
                yield return null;
            }

            string[] split = draw.drawString.Split('\n');

            split = split.Where(x => !x.Contains("Mono.CSharp.dll: FAILED TO LOAD! Check ModLog.txt")).ToArray();

            draw.drawString = String.Join("\n", split);
        }
    }
}