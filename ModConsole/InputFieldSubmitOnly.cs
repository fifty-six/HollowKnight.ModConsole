using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ModConsole
{
    /// <summary>
    /// Only submits when [Enter] is pressed, instead of submitting on loss of focus.
    /// </summary>
    public class InputFieldSubmitOnly : InputField
    {
        public override void OnDeselect(BaseEventData eventData)
        {
            interactable = false;
            base.OnDeselect(eventData);
            interactable = true;
        }
    }
}