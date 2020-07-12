using Modding;
using UnityEngine;
using UnityEngine.UI;

namespace ModConsole
{
    internal static class Canvas
    {
        public static (GameObject textPanel, GameObject interactiveTextPanel) CreatePanels(Font font, GameObject bg, GameObject interact_bg)
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

        public static (GameObject bg, GameObject interact_bg) CreateBackgrounds(GameObject canvas)
        {
            GameObject bg = CanvasUtil.CreateImagePanel
            (
                canvas,
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
                canvas,
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
    }
}