﻿﻿using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Projects.Scripts.Control
{
    public class InputManager : MonoBehaviour
    {
        [SerializeField] private RawImage rawImage;
        [SerializeField] private Camera renderTextureCamera;
        [SerializeField] private LayerMask raycastMask = Physics2D.DefaultRaycastLayers;

        private IInputHandler _currentHandler;
        private LayerMask _defaultRaycastMask;

        private void Awake()
        {
            if (raycastMask.value == 0)
            {
                raycastMask = Physics2D.DefaultRaycastLayers;
            }

            _defaultRaycastMask = raycastMask;
        }
        
        private void Update()
        {
            var phase = GetInputPhase(out var screenPosition);

            switch (phase)
            {
                case InputPhase.Began:
                    HandleInputBegan(screenPosition);
                    break;
                case InputPhase.Held:
                    if (_currentHandler != null && TryGetWorldPosition(screenPosition, out var dragWorldPos))
                        _currentHandler.OnInputDrag(dragWorldPos);
                    break;
                case InputPhase.Ended:
                    if (_currentHandler != null && TryGetWorldPosition(screenPosition, out var endWorldPos))
                        _currentHandler.OnInputEnd(endWorldPos);
                    _currentHandler = null;
                    break;
                case InputPhase.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void HandleInputBegan(Vector2 screenPosition)
        {
            if (!TryRaycastHandlerObject(screenPosition, out var draggable)) return;

            _currentHandler = draggable;
            if (TryGetWorldPosition(screenPosition, out var worldPos))
                _currentHandler.OnInputBegin(worldPos);
        }

        /// <summary>
        /// スクリーン座標をRenderTextureカメラ経由でワールド座標に変換する
        /// </summary>
        private bool TryGetWorldPosition(Vector2 screenPosition, out Vector2 worldPosition)
        {
            worldPosition = Vector2.zero;
            if (!TryGetRenderTextureScreenPoint(screenPosition, out var rtScreenPoint)) return false;
            worldPosition = renderTextureCamera.ScreenToWorldPoint(rtScreenPoint);
            return true;
        }

        /// <summary>
        /// スクリーン座標からRenderTextureカメラを通してIInputHandlerをレイキャストする
        /// </summary>
        private bool TryRaycastHandlerObject(Vector2 screenPosition, out IInputHandler handler)
        {
            handler = null;

            if (!TryGetRenderTextureScreenPoint(screenPosition, out var worldPoint)) return false;

            var cameraRay = renderTextureCamera.ScreenPointToRay(worldPoint);
            var raycastHit = Physics2D.GetRayIntersection(cameraRay, Mathf.Infinity, raycastMask);

            if (raycastHit.collider == null) return false;
            return raycastHit.collider.TryGetComponent(out handler);
        }

        public void SetRaycastMaskOverride(LayerMask overrideMask)
        {
            raycastMask = overrideMask;
        }

        public void ClearRaycastMaskOverride()
        {
            raycastMask = _defaultRaycastMask;
        }

        private enum InputPhase
        {
            None,
            Began,
            Held,
            Ended,
        }

        /// <summary>
        /// 現在の入力フェーズとスクリーン座標を取得する（タッチ優先）
        /// </summary>
        private static InputPhase GetInputPhase(out Vector2 screenPosition)
        {
            screenPosition = Vector2.zero;

            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var primaryTouch = touchscreen.primaryTouch;
                if (primaryTouch.press.wasPressedThisFrame ||
                    primaryTouch.press.isPressed ||
                    primaryTouch.press.wasReleasedThisFrame)
                {
                    screenPosition = primaryTouch.position.ReadValue();
                    if (primaryTouch.press.wasPressedThisFrame) return InputPhase.Began;
                    if (primaryTouch.press.wasReleasedThisFrame) return InputPhase.Ended;
                    return InputPhase.Held;
                }
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame ||
                    mouse.leftButton.isPressed ||
                    mouse.leftButton.wasReleasedThisFrame)
                {
                    screenPosition = mouse.position.ReadValue();
                    if (mouse.leftButton.wasPressedThisFrame) return InputPhase.Began;
                    if (mouse.leftButton.wasReleasedThisFrame) return InputPhase.Ended;
                    return InputPhase.Held;
                }
            }

            return InputPhase.None;
        }

        /// <summary>
        /// スクリーン座標をRenderTexture上のスクリーン座標に変換する。
        /// Canvas が Screen Space - Overlay の場合にも対応。
        /// </summary>
        private bool TryGetRenderTextureScreenPoint(Vector2 screenPosition, out Vector3 renderTextureScreenPoint)
        {
            renderTextureScreenPoint = Vector3.zero;
            var rectTransform = rawImage.rectTransform;

            // Overlay Canvas では worldCamera が null になるが、
            // ScreenPointToLocalPointInRectangle は null カメラで正しく動作する
            var canvas = rawImage.canvas;
            var canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
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

            renderTextureScreenPoint = new Vector3(
                normalizedPoint.x * renderTexture.width,
                normalizedPoint.y * renderTexture.height,
                renderTextureCamera.nearClipPlane
            );

            return true;
        }
    }
}
