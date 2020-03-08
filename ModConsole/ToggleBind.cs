using Modding;
using UnityEngine;

namespace ModConsole
{
    public class ToggleBind : MonoBehaviour
    {
        private CanvasGroup _group;
        
        public GameObject Canvas { get; set; }

        private void Start()
        {
            _group = Canvas.GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.F9)) return;
            
            StartCoroutine
            (
                Canvas.activeSelf
                    ? CanvasUtil.FadeOutCanvasGroup(_group)
                    : CanvasUtil.FadeInCanvasGroup(_group)
            );
        }
    }
}