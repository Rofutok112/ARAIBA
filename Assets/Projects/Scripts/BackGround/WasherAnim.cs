using UnityEngine;
using DG.Tweening;

namespace Projects.Scripts.BackGround
{
    public class WasherAnim : MonoBehaviour
    {
        [SerializeField] private float duration = 0.3f;
        [SerializeField] private float strength = 0.08f;
        [SerializeField] private int vibrato = 20;
        [SerializeField] private float randomness = 0f;

        private Tween _shakeTween;
        private Vector3 _defaultLocalPosition;

        private void Awake()
        {
            _defaultLocalPosition = transform.localPosition;
        }

        private void OnDisable()
        {
            StopVibration();
        }

        private void OnDestroy()
        {
            StopVibration();
        }

        public void StartVibration()
        {
            if (_shakeTween is { active: true })
            {
                return;
            }
            
            transform.localPosition = _defaultLocalPosition;
            _shakeTween = transform.DOShakePosition(duration, new Vector3(strength, strength, 0f), vibrato, randomness)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        public void StopVibration()
        {
            if (_shakeTween != null)
            {
                _shakeTween.Kill();
                _shakeTween = null;
            }

            transform.localPosition = _defaultLocalPosition;
        }
    }
}
