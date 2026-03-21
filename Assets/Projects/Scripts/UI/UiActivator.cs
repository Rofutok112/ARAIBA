using UnityEngine;

namespace Projects.Scripts.UI
{
    public class UiActivator : MonoBehaviour
    {
        [SerializeField] private GameObject[] uiElement;

        public void Activate()
        {
            if (uiElement == null || uiElement.Length == 0)
            {
                Debug.LogWarning("UiActivator: No UI elements assigned.");
                return;
            }
            
            foreach (var element in uiElement)
            {
                element.SetActive(true);
            }
        }

        public void Deactivate()
        {
            if (uiElement == null || uiElement.Length == 0)
            {
                Debug.LogWarning("UiActivator: No UI elements assigned.");
                return;
            }

            foreach (var element in uiElement)
            {
                element.SetActive(false);
            }
        }
    }
}
