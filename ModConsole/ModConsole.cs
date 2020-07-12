using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        internal static ModConsole Instance;

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
            Instance = this;

            Font font = FontUtil.LoadFont(_settings.Font);

            (ConsoleInputField input, Text consoleText) = SetupCanvas(font);

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
                        AddMessage($"=> {Inspector.Inspect(output)}");
                    }
                }
            );
        }

        private (ConsoleInputField inp, Text text) SetupCanvas(Font font)
        {
            _canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920, 1080));
            _canvas.name = "Console";

            _toggle = new GameObject("Toggler");

            var toggle = _toggle.AddComponent<ToggleBind>();

            toggle.Canvas = _canvas;
            toggle.StartCoroutine(FontUtil.ChangeAPIFont(font));

            UObject.DontDestroyOnLoad(_canvas);
            UObject.DontDestroyOnLoad(toggle);

            (GameObject bg, GameObject interact_bg) = Canvas.CreateBackgrounds(_canvas);

            (GameObject textPanel, GameObject interactiveTextPanel) = Canvas.CreatePanels(font, bg, interact_bg);

            var consoleText = textPanel.GetComponent<Text>();

            consoleText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var ibox = new GameObject("Input Box");

            // Parent the input box to the canvas
            ibox.transform.parent = _canvas.transform;

            var input = interactiveTextPanel.AddComponent<ConsoleInputField>();
            input.interactable = true;
            input.textComponent = interactiveTextPanel.GetComponent<Text>();
            input.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;

            return (input, consoleText);
        }

        private static IEnumerable<string> Chunks(string str, int maxChunkSize)
        {
            for (int i = 0; i < str.Length; i += maxChunkSize)
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
        }

        public void Unload()
        {
            UObject.Destroy(_canvas);
            UObject.Destroy(_toggle);
        }
    }
}