using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Projects.Scripts.Touch
{
    public class TouchManager : MonoBehaviour
    {
        [SerializeField] private RawImage rawImage;
        [SerializeField] private Camera renderTextureCamera;

        private void Update()
        {
            if (!TryGetPressedScreenPosition(out var screenPosition)) return;

            if (!TryGetWorldPoint(screenPosition, out var worldPoint)) return;

            var cameraRay = renderTextureCamera.ScreenPointToRay(worldPoint);

            var raycastHit = Physics2D.GetRayIntersection(cameraRay);
            if (raycastHit.collider == null) return;

            if (!raycastHit.collider.TryGetComponent<TouchableObject>(out var obj)) return;
            
            obj.Execute();
        }

        /// <summary>
        /// タッチスクリーンまたはマウスの押下位置を取得する
        /// </summary>
        private static bool TryGetPressedScreenPosition(out Vector2 screenPosition)
        {
            screenPosition = Vector2.zero;

            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var primaryTouch = touchscreen.primaryTouch;
                if (primaryTouch.press.wasPressedThisFrame)
                {
                    screenPosition = primaryTouch.position.ReadValue();
                    return true;
                }
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screenPosition = mouse.position.ReadValue();
                return true;
            }

            return false;
        }

        /// <summary>
        /// スクリーン座標をRenderTexture上のワールド座標に変換する
        /// </summary>
        private bool TryGetWorldPoint(Vector2 screenPosition, out Vector3 worldPoint)
        {
            worldPoint = Vector3.zero;
            var rectTransform = rawImage.rectTransform;

            // スクリーン座標 → RawImageローカル座標
            var canvasCamera = rawImage.canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, screenPosition, canvasCamera, out var localPoint))
                return false;

            // ローカル座標 → RenderTexture座標（0〜1の正規化座標）
            var rect = rectTransform.rect;
            var normalizedPoint = new Vector2(
                (localPoint.x - rect.x) / rect.width,
                (localPoint.y - rect.y) / rect.height
            );

            // RenderTexture範囲外は無視
            if (normalizedPoint.x < 0 || normalizedPoint.x > 1 ||
                normalizedPoint.y < 0 || normalizedPoint.y > 1)
                return false;

            // RenderTexture座標 → Camera.ScreenToWorldPoint用のスクリーン座標に変換
            var renderTexture = rawImage.texture as RenderTexture;
            if (renderTexture == null) return false;

            worldPoint = new Vector3(
                normalizedPoint.x * renderTexture.width,
                normalizedPoint.y * renderTexture.height,
                renderTextureCamera.nearClipPlane
            );

            return true;
        }
    }
}