using UnityEngine;

namespace Projects.Scripts.Control
{
    /// <summary>
    /// インタラクト可能な対象に対してアウトラインの呼吸演出を与える。
    /// 対象 SpriteRenderer には Hint 対応 shader/material を割り当てる前提。
    /// </summary>
    [DisallowMultipleComponent]
    public class InteractionHintPulse : MonoBehaviour
    {
        private static readonly int HintEnabledId = Shader.PropertyToID("_HintEnabled");
        private static readonly int PulseId = Shader.PropertyToID("_Pulse");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
        private static readonly int GlowStrengthId = Shader.PropertyToID("_GlowStrength");

        [Header("Animation")]
        [SerializeField, Min(0.1f)] private float pulseSpeed = 1.1f;
        [SerializeField] private Vector2 pulseRange = new(0.15f, 0.8f);

        [Header("Outline")]
        [SerializeField] private Color outlineColor = new(0.85f, 0.95f, 1f, 1f);
        [SerializeField, Min(0f)] private float outlineWidth = 2f;
        [SerializeField, Min(0f)] private float glowStrength = 1f;
        [SerializeField] private Material hintMaterial;
        [SerializeField] private string hintChildName = "InteractionHintVisual";
        [SerializeField] private bool useDiagonalOffsets = true;

        private MaterialPropertyBlock _propertyBlock;
        private IInteractionHintTarget _hintTarget;
        private SpriteRenderer _sourceRenderer;
        private readonly SpriteRenderer[] _hintRenderers = new SpriteRenderer[8];
        private readonly Transform[] _hintTransforms = new Transform[8];

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _hintTarget = GetComponent<IInteractionHintTarget>();
            _sourceRenderer = _hintTarget != null ? _hintTarget.HintSpriteRenderer : null;
            if (_sourceRenderer == null)
            {
                _sourceRenderer = GetComponent<SpriteRenderer>();
            }

            if (_sourceRenderer != null)
            {
                EnsureHintRenderers();
                SyncHintRenderers();
                ApplyHint(0f, false);
            }
        }

        private void LateUpdate()
        {
            if (_sourceRenderer == null)
            {
                return;
            }

            SyncHintRenderers();

            var shouldShow = _hintTarget != null && _hintTarget.ShouldShowInteractionHint;
            if (!shouldShow)
            {
                ApplyHint(0f, false);
                return;
            }

            var normalized = Mathf.InverseLerp(-1f, 1f, Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f));
            var pulse = Mathf.Lerp(pulseRange.x, pulseRange.y, normalized);
            ApplyHint(pulse, true);
        }

        private void ApplyHint(float pulse, bool enabled)
        {
            for (var i = 0; i < _hintRenderers.Length; i++)
            {
                var hintRenderer = _hintRenderers[i];
                if (hintRenderer == null) continue;

                var shouldEnableRenderer = enabled && (useDiagonalOffsets || i < 4);
                hintRenderer.enabled = shouldEnableRenderer;
                if (!shouldEnableRenderer) continue;

                hintRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(HintEnabledId, 1f);
                _propertyBlock.SetFloat(PulseId, pulse);
                _propertyBlock.SetColor(OutlineColorId, outlineColor);
                _propertyBlock.SetFloat(OutlineWidthId, outlineWidth);
                _propertyBlock.SetFloat(GlowStrengthId, glowStrength);
                hintRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private void EnsureHintRenderers()
        {
            for (var i = 0; i < _hintRenderers.Length; i++)
            {
                var childName = $"{hintChildName}_{i}";
                var existingChild = transform.Find(childName);
                Transform hintTransform;
                if (existingChild == null)
                {
                    var child = new GameObject(childName);
                    hintTransform = child.transform;
                    hintTransform.SetParent(transform, false);
                }
                else
                {
                    hintTransform = existingChild;
                }

                var hintRenderer = hintTransform.GetComponent<SpriteRenderer>();
                if (hintRenderer == null)
                {
                    hintRenderer = hintTransform.gameObject.AddComponent<SpriteRenderer>();
                }

                if (hintMaterial != null)
                {
                    hintRenderer.sharedMaterial = hintMaterial;
                }

                _hintTransforms[i] = hintTransform;
                _hintRenderers[i] = hintRenderer;
            }
        }

        private void SyncHintRenderers()
        {
            var offsets = GetLocalOffsets();
            for (var i = 0; i < _hintRenderers.Length; i++)
            {
                var hintTransform = _hintTransforms[i];
                var hintRenderer = _hintRenderers[i];
                if (hintTransform == null || hintRenderer == null) continue;

                hintTransform.localPosition = offsets[i];
                hintTransform.localRotation = Quaternion.identity;
                hintTransform.localScale = Vector3.one;

                hintRenderer.sprite = _sourceRenderer.sprite;
                hintRenderer.flipX = _sourceRenderer.flipX;
                hintRenderer.flipY = _sourceRenderer.flipY;
                hintRenderer.drawMode = _sourceRenderer.drawMode;
                hintRenderer.size = _sourceRenderer.size;
                hintRenderer.sortingLayerID = _sourceRenderer.sortingLayerID;
                hintRenderer.sortingOrder = _sourceRenderer.sortingOrder - 1;
                hintRenderer.maskInteraction = _sourceRenderer.maskInteraction;
                if (hintMaterial != null)
                {
                    hintRenderer.sharedMaterial = hintMaterial;
                }
            }
        }

        private Vector3[] GetLocalOffsets()
        {
            var step = Mathf.Max(0.001f, outlineWidth * 0.01f);
            return new[]
            {
                new Vector3(step, 0f, 0f),
                new Vector3(-step, 0f, 0f),
                new Vector3(0f, step, 0f),
                new Vector3(0f, -step, 0f),
                new Vector3(step, step, 0f),
                new Vector3(-step, step, 0f),
                new Vector3(step, -step, 0f),
                new Vector3(-step, -step, 0f),
            };
        }
    }
}
