using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Modding;
using JetBrains.Annotations;
using Mono.CSharp;
using UnityEngine;
using UnityEngine.UI;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using UObject = UnityEngine.Object;

namespace ModConsole
{
    [UsedImplicitly]
    public class ModConsole : Mod, ITogglableMod
    {
        public override ModSettings GlobalSettings
        {
            get => _settings;
            set => _settings = value as GlobalSettings;
        }

        private GlobalSettings _settings = new GlobalSettings();

        private GameObject _canvas;
        private GameObject _toggle;
        
        private Evaluator _eval;

        private readonly List<string> _messages = new List<string>();

        private readonly string[] USINGS =
        {
            "using UnityEngine;",
            "using UnityEngine.UI;",
            "using Modding;",
            "using System;",
            "using System.Collections;",
            "using System.Collections.Generic;",
            "using System.Reflection;",
            "using System.Linq;",
            "using USceneManager = UnityEngine.SceneManagement.SceneManager;",
            "using UObject = UnityEngine.Object;",
        };

        private const int LINE_COUNT = 40;

        public override string GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public override void Initialize()
        {
            Font font = LoadFont();

            _canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920, 1080));
            _canvas.name = "Console";

            _toggle = new GameObject("Toggler");

            var toggle = _toggle.AddComponent<ToggleBind>();

            toggle.Canvas = _canvas;
            toggle.StartCoroutine(ChangeAPIFont(font));

            UObject.DontDestroyOnLoad(_canvas);
            UObject.DontDestroyOnLoad(toggle);

            (GameObject bg, GameObject interact_bg) = CreateBackgrounds();

            (GameObject textPanel, GameObject interactiveTextPanel) = CreatePanels(font, bg, interact_bg);

            var consoleText = textPanel.GetComponent<Text>();

            consoleText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var ibox = new GameObject("Input Box");

            // Parent the input box to the canvas
            ibox.transform.parent = _canvas.transform;

            var input = interactiveTextPanel.AddComponent<ConsoleInputField>();
            input.interactable = true;
            input.textComponent = interactiveTextPanel.GetComponent<Text>();
            input.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;

            void AddMessage(string message)
            {
                string[] lines = message.Split('\n');
                
                foreach (string line in lines)
                {
                    IEnumerable<string> chunks = Chunks(line, 80);
                    
                    _messages.AddRange(chunks);
                }
                
                while (_messages.Count > LINE_COUNT)
                    _messages.RemoveAt(0);
                
                consoleText.text = string.Join("\n", _messages.ToArray());
            }

            var writer = new LambdaWriter(AddMessage);

            _eval = new Evaluator
            (
                new CompilerContext
                (
                    new CompilerSettings
                    {
                        AssemblyReferences = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.FullName).ToList(),
                    },
                    new ConsoleReportPrinter(writer)
                )
            );

            foreach (string @using in USINGS)
            {
                // It throws an ArgumentException for any using, but it succeeds regardless
                _eval.TryEvaluate(@using, out object _);
            }
            
            input.onEndEdit.AddListener
            (
                str =>
                {
                    if (string.IsNullOrEmpty(str)) return;

                    AddMessage(str);

                    if (_eval.TryEvaluate(str, out object output))
                    {
                        AddMessage($"=> {Inspect(output)}");
                    }
                }
            );
        }

        private static (GameObject textPanel, GameObject interactiveTextPanel) CreatePanels(Font font, GameObject bg, GameObject interact_bg)
        {
            GameObject textPanel = CanvasUtil.CreateTextPanel
            (
                bg,
                "",
                12,
                TextAnchor.LowerLeft,
                new CanvasUtil.RectData
                (
                    // No size change, anchor at bottom left, go from bottom-left to top-right
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.one
                ),
                font
            );

            GameObject interactiveTextPanel = CanvasUtil.CreateTextPanel
            (
                interact_bg,
                "[Console Input]",
                12,
                TextAnchor.LowerLeft,
                new CanvasUtil.RectData
                (
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.one
                ),
                font
            );

            return (textPanel, interactiveTextPanel);
        }

        private (GameObject bg, GameObject interact_bg) CreateBackgrounds()
        {
            GameObject bg = CanvasUtil.CreateImagePanel
            (
                _canvas,
                CanvasUtil.NullSprite(new byte[] {0, 0, 0, 64}),
                new CanvasUtil.RectData
                (
                    new Vector2(600, 600),
                    // Anchor bottom left to the top-right minus the size 
                    new Vector2(1920 - 600, 1080 - 600),
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.zero
                )
            );

            bg.GetComponent<Image>().preserveAspect = false;

            GameObject interact_bg = CanvasUtil.CreateImagePanel
            (
                _canvas,
                CanvasUtil.NullSprite(new byte[] {0, 0, 0, 96}),
                new CanvasUtil.RectData
                (
                    new Vector2(600, 600),
                    // Same anchor as before, but minus the (y) size again so it's beneath the previous box.
                    new Vector2(1920 - 600, 1080 - 600 - 20),
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.zero
                )
            );

            return (bg, interact_bg);
        }

        private Font LoadFont()
        {
            Assembly asm = Assembly.GetExecutingAssembly();

            // If Fira Code fails to load for whatever reason, Arial is a good backup
            var font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;

            if (!string.IsNullOrEmpty(_settings.Font))
            {
                font = Font.CreateDynamicFontFromOSFont(_settings.Font, 1);

                if (font.name == "Arial")
                    LogWarn($"Unable to find font {_settings.Font}, falling back to Fira Code.");
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

        private static IEnumerator ChangeAPIFont(Font font)
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

            draw.drawString = string.Join("\n", split);
        }

        private static string Inspect(object result, InspectionType type = InspectionType.Fields)
        {
            switch (result) 
            {
                case null:
                    return "null";
                
                case string str:
                    return str;
            }

            Type rType = result.GetType();

            var sb = new StringBuilder();

            sb.AppendLine($"[{rType}]");
            sb.AppendLine($"{result}");

            FieldInfo[] fields = rType.GetFields(BindingFlags.Public | BindingFlags.Instance).OrderBy(x => x.Name).ToArray();
            PropertyInfo[] properties = rType.GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(x => x.Name).ToArray();

            if (fields.Length == 0)
                type = InspectionType.Properties;

            if (properties.Length == 0)
                type = InspectionType.Fields;

            switch (type)
            {
                case InspectionType.Fields:
                    foreach (FieldInfo field in fields)
                        AppendMemberInfo(result, field, sb);
                    break;
                case InspectionType.Properties:
                    foreach (PropertyInfo property in properties)
                        AppendMemberInfo(result, property, sb);
                    break;
            }

            return sb.ToString();
        }

        private static void AppendMemberInfo(object result, MemberInfo member, StringBuilder sb)
        {
            object value = null;

            try
            {
                switch (member)
                {
                    case PropertyInfo p:
                    {
                        // Indexer propertty
                        if (p.GetIndexParameters().Length != 0)
                            return;

                        value = p.GetValue(result, null);
                        break;
                    }

                    case FieldInfo fi:
                    {
                        value = fi.GetValue(result);
                        break;
                    }
                };
            }
            catch (TargetInvocationException)
            {
                // yeet 
            }

            sb.Append("<color=#14f535>").Append(member.Name.PadRight(30)).Append("</color>");

            switch (value)
            {
                case string s:
                    sb.AppendLine(s);
                    break;
                case IEnumerable e:
                    IEnumerable<object> collection = e.Cast<object>();

                    // Don't have multiple enumerations
                    IEnumerable<object> enumerated = collection as object[] ?? collection.ToArray();

                    int count = enumerated.Count();

                    Type type = enumerated.FirstOrDefault()?.GetType();

                    if ((type?.IsPrimitive ?? false) || type == typeof(string))
                    {
                        sb.Append("[");
                        
                        sb.Append(string.Join(", ", enumerated.Take(Math.Min(5, count)).Select(x => x.ToString()).ToArray()));

                        if (count > 5)
                            sb.Append(", ...");

                        sb.AppendLine("]");
                    }
                    else
                    {
                        sb.AppendLine($"Item Count: {count}");
                    }

                    break;
                default:
                    sb.AppendLine(value?.ToString() ?? "null");
                    break;
            }
        }
        
        private static IEnumerable<string> Chunks(string str, int maxChunkSize) 
        {
            for (int i = 0; i < str.Length; i += maxChunkSize) 
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length-i));
        }

        public void Unload()
        {
            UObject.Destroy(_canvas);
            UObject.Destroy(_toggle);
        }
    }

    internal enum InspectionType
    {
        Fields,
        Properties
    }
}