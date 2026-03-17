using Projects.Scripts.Control;
using UnityEngine;
using DG.Tweening;

namespace Projects.Scripts.InteractiveObjects
{
    public class Rack : MonoBehaviour, IInputHandler
    {
        [SerializeField] private GameObject puzzleWindow;
        [SerializeField] private GameObject puzzleUI;
        
        [SerializeField] private Vector3 openPositionOffset = new(0f, 0.5f, 0f);
        [SerializeField] private float openAnimationDuration = 0.5f;
        
        public void OnInputBegin(Vector2 pos)
        {
            var defaultPos = puzzleWindow.transform.position;
            puzzleWindow.transform.position = defaultPos - openPositionOffset;
            puzzleWindow.SetActive(true);
            puzzleWindow.transform.DOMove(defaultPos, openAnimationDuration).SetEase(Ease.OutBack);
            puzzleUI.SetActive(true);
        }

        public void OnInputDrag(Vector2 pos) { }

        public void OnInputEnd(Vector2 pos) { }
    }
}
