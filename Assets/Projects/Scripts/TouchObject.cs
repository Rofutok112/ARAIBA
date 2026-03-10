using UnityEngine;
using UnityEngine.InputSystem;

namespace Projects.Scripts
{
    public class TouchObject : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        private readonly IExecutableOnTouch _behaviour;
        private void Update()
        {
            var screen = Touchscreen.current;

            if (screen == null) return;
            var touch = screen.primaryTouch;

            if (!touch.press.wasPressedThisFrame) return;
            
            // タッチした座標を取得
            var screenPos = touch.position.ReadValue();
            var ray = mainCamera.ScreenPointToRay(screenPos);
            
            // タッチした座標上のオブジェクトを取得
            if (!Physics.Raycast(ray, out var hit)) return;
            if (hit.collider.gameObject == this.gameObject)
            {
                // タッチしたオブジェクト固有の処理を実行
                _behaviour.Execute();
            }
        }
    }
}
