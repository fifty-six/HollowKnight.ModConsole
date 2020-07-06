using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ModConsole
{
    /// <summary>
    /// Only submits when [Enter] is pressed, instead of submitting on loss of focus.
    /// </summary>
    public class ConsoleInputField : InputField
    {
        private readonly List<string> _history = new List<string>();

        private int _histInd;
        
        public override void OnDeselect(BaseEventData eventData)
        {
            interactable = false;
            
            base.OnDeselect(eventData);
            
            interactable = true;
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            text = string.Empty;
            
            _history.Add(text);
            
            _histInd = _history.Count;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                if (_histInd - 1 < 0)
                    return;
                
                text = _history[--_histInd];
            }

            // ReSharper disable once InvertIf
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                if (_histInd + 1 >= _history.Count)
                    return;

                text = _history[++_histInd];
            }
        }
    }
}