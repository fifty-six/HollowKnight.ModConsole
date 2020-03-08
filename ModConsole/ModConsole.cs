﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        private GameObject _canvas;

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

        private const int COUNT = 24;

        public override void Initialize()
        {
            Font font = LoadFont();

            _canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920, 1080));
            _canvas.name = "Console";

            var toggle = new GameObject("Toggler").AddComponent<ToggleBind>();

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

            var input = interactiveTextPanel.AddComponent<InputFieldSubmitOnly>();
            input.interactable = true;
            input.textComponent = interactiveTextPanel.GetComponent<Text>();
            input.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;

            void AddMessage(string message)
            {
                if (_messages.Count > COUNT)
                {
                    _messages.RemoveAt(0);
                }

                _messages.Add(message);

                consoleText.text = string.Join("\n", _messages.ToArray());
            }

            var writer = new LambdaWriter(AddMessage);
            
            var eval = new Evaluator
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
                eval.TryEvaluate(@using, out object _);
            }

            input.onEndEdit.AddListener
            (
                str =>
                {
                    if (string.IsNullOrEmpty(str)) return;

                    AddMessage(str);

                    if (eval.TryEvaluate(str, out object output))
                    {
                        AddMessage($"=> {output ?? "null"}");
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
                    // anchor bottom left to the top-right minus the size 
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
                    new Vector2(600, 20),
                    // Same anchor as before, but minus the (y) size again so it's beneath the previous box.
                    new Vector2(1920 - 600, 1080 - 600 - 20),
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero
                )
            );
            
            return (bg, interact_bg);
        }

        private static Font LoadFont()
        {
            Assembly asm = Assembly.GetExecutingAssembly();

            // If Consolas fails to load for whatever reason, Arial is a good backup
            var font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;

            foreach (string res in asm.GetManifestResourceNames())
            {
                using (Stream s = asm.GetManifestResourceStream(res))
                {
                    AssetBundle ab = AssetBundle.LoadFromStream(s);

                    var consolas = ab.LoadAsset<Font>("Consolas.ttf");
                    
                    if (consolas != null)
                        font = consolas;
                }
            }

            return font;
        }

        private static IEnumerator ChangeAPIFont(Font font)
        {
            GameObject console;

            while ((console = GameObject.Find("ModdingApiConsoleLog")) == null)
                yield return null;

            console.GetComponentInChildren<Text>(true).font = font;

            // Hide the failed to load error for Mono.CSharp because it actually works anyways and confusing people is annoying.
            Type modLoader = Type.GetType("Modding.ModLoader, Assembly-CSharp");

            if (modLoader == null) yield break;

            var draw = (ModVersionDraw) modLoader.GetField("_draw", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);

            if (draw == null) yield break;

            string[] split = draw.drawString.Split('\n');

            split = split.Where(x => !x.Contains("Mono.CSharp.dll: FAILED TO LOAD! Check ModLog.txt")).ToArray();

            draw.drawString = string.Join("\n", split);
        }

        public void Unload()
        {
            UObject.Destroy(_canvas);
        }
    }
}