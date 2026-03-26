using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Projects.Scripts.UI
{
    public class DefaultGameResultAnimation : MonoBehaviour, IGameResultAnimationDriver
    {
        [SerializeField, Min(0.1f)] private float overlayFadeDuration = 0.3f;
        [SerializeField, Min(0.1f)] private float panelPopDuration = 0.45f;
        [SerializeField, Min(0f)] private float sectionStagger = 0.08f;
        [SerializeField, Min(0.1f)] private float headerFadeDuration = 0.18f;
        [SerializeField, Min(0.1f)] private float bodyFadeDuration = 0.2f;
        [SerializeField, Min(0.1f)] private float buttonsFadeDuration = 0.18f;
        [SerializeField, Min(0.1f)] private float scorePulseDuration = 0.28f;
        [SerializeField, Min(0f)] private float formulaHoldDuration = 0.45f;
        [SerializeField] private Vector3 hiddenPanelScale = new(0.88f, 0.88f, 1f);
        [SerializeField] private Vector2 hiddenPanelOffset = new(0f, -28f);
        [SerializeField, Min(1f)] private float scorePulseScale = 1.08f;

        private Sequence _activeSequence;

        public void ResetVisuals(GameResultAnimationTargets targets)
        {
            _activeSequence?.Kill();
            _activeSequence = null;

            targets.OverlayGroup.alpha = 0f;
            targets.HeaderGroup.alpha = 0f;
            targets.BodyGroup.alpha = 0f;
            targets.ButtonsGroup.alpha = 0f;
            targets.PanelTransform.localScale = hiddenPanelScale;
            targets.PanelTransform.anchoredPosition = hiddenPanelOffset;

            if (targets.FinalScoreText != null)
            {
                targets.FinalScoreText.text = targets.FinalScoreFormulaText;
                targets.FinalScoreText.rectTransform.localScale = Vector3.one;
            }
        }

        public async UniTask PlayShowAsync(GameResultAnimationTargets targets)
        {
            _activeSequence?.Kill();

            var scoreHalfDuration = scorePulseDuration * 0.5f;
            var sequence = DOTween.Sequence().SetUpdate(true);
            var completionSource = new UniTaskCompletionSource();

            sequence.Append(targets.OverlayGroup.DOFade(1f, overlayFadeDuration).SetEase(Ease.OutCubic));
            sequence.Append(targets.PanelTransform.DOScale(Vector3.one, panelPopDuration).SetEase(Ease.OutBack));
            sequence.Join(targets.PanelTransform.DOAnchorPos(Vector2.zero, panelPopDuration).SetEase(Ease.OutBack));
            sequence.Append(targets.HeaderGroup.DOFade(1f, headerFadeDuration).SetEase(Ease.OutCubic));
            sequence.AppendInterval(sectionStagger);
            sequence.Append(targets.BodyGroup.DOFade(1f, bodyFadeDuration).SetEase(Ease.OutCubic));
            sequence.AppendInterval(sectionStagger);
            sequence.Append(targets.ButtonsGroup.DOFade(1f, buttonsFadeDuration).SetEase(Ease.OutCubic));

            if (targets.FinalScoreText != null)
            {
                sequence.AppendInterval(formulaHoldDuration);
                sequence.AppendCallback(() => targets.FinalScoreText.text = targets.FinalScoreResolvedText);
                sequence.Append(targets.FinalScoreText.rectTransform.DOScale(Vector3.one * scorePulseScale, scoreHalfDuration).SetEase(Ease.OutQuad));
                sequence.Append(targets.FinalScoreText.rectTransform.DOScale(Vector3.one, scoreHalfDuration).SetEase(Ease.InQuad));
            }

            sequence.OnComplete(() => completionSource.TrySetResult());
            sequence.OnKill(() => completionSource.TrySetResult());
            _activeSequence = sequence;

            await completionSource.Task;

            if (_activeSequence == sequence)
            {
                _activeSequence = null;
            }
        }
    }
}
