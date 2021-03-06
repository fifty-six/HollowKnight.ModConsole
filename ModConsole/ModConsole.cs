﻿using System;
using System.Collections.Generic;
using System.Linq;
using Modding;
using JetBrains.Annotations;
using Mono.CSharp;
using UnityEngine;
using UnityEngine.UI;
using Vasi;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using UObject = UnityEngine.Object;

namespace ModConsole
{
    [UsedImplicitly]
    public class ModConsole : Mod, ITogglableMod
    {
        [UsedImplicitly]
        public static ModConsole Instance { get; set; }

        [UsedImplicitly]
        public Action<string> LogToConsole { get; set; }
        
        private GameObject _canvas;
        private GameObject _toggle;

        private Evaluator _eval;

        private readonly List<string> _messages = new();

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

        public override string GetVersion() => VersionUtil.GetVersion<ModConsole>();

        public override void Initialize()
        {
            Instance = this;
            
            (ConsoleInputField input, Text consoleText) = SetupCanvas(FontUtil.Font);

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

            LogToConsole = AddMessage;

            foreach (string @using in USINGS)
            {
                // It throws an ArgumentException for any using, but it succeeds regardless
                _eval.TryEvaluate(@using, out object _);
            }
            
            _eval.TryEvaluate("Action<string> Log = ModConsole.ModConsole.Instance.LogToConsole", out _);

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
            {
                string chunk = str.Substring(i, Math.Min(maxChunkSize, str.Length - i));

                if (chunk.Contains("<color="))
                {
                    int begin_tag = chunk.IndexOf("<color=");
                    
                    // Find the end of the initial tag
                    int tag = i + chunk.IndexOf("\">", begin_tag) + 2;
                    
                    // End tag
                    int end_tag = chunk.IndexOf("</color>", tag);
                    
                    // Length of the tags
                    int inital_tag_len = tag == -1 ? "<color=\"".Length : begin_tag - tag;
                    int end_tag_len = end_tag == -1 ? 0 : "</color".Length;

                    // Same chunk, but now we don't include the tag length in the chunk size.
                    yield return str.Substring(i, Math.Min(maxChunkSize + inital_tag_len + end_tag_len, str.Length - i));

                    // Have to increment the i
                    i += inital_tag_len;
                    i += end_tag_len;

                    continue;
                }

                yield return chunk;
            }
        }

        public void Unload()
        {
            UObject.Destroy(_canvas);
            UObject.Destroy(_toggle);
        }
    }
}