using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Projects.Scripts.UI
{
    public class GameResultPresenter : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject rootObject;
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private RectTransform panelTransform;

        [Header("Animation Groups")]
        [SerializeField] private CanvasGroup headerGroup;
        [SerializeField] private CanvasGroup bodyGroup;
        [SerializeField] private CanvasGroup buttonsGroup;

        [Header("Texts")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text pieceScoreValueText;
        [SerializeField] private TMP_Text washerTimeValueText;
        [SerializeField] private TMP_Text utilizationValueText;
        [SerializeField] private TMP_Text finalScoreValueText;
        [SerializeField] private TMP_Text bestScoreValueText;

        [Header("Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button titleButton;

        [Header("Animation")]
        [SerializeField] private MonoBehaviour animationDriverBehaviour;

        private IGameResultAnimationDriver _animationDriver;

        private void Awake()
        {
            _animationDriver = animationDriverBehaviour as IGameResultAnimationDriver;

            if (rootObject != null)
            {
                rootObject.SetActive(false);
            }
        }

        public void Show(GameResultSummary summary, Action onRetry, Action onTitle)
        {
            if (!ValidateReferences())
            {
                return;
            }

            titleText.text = "TIME UP";
            pieceScoreValueText.text = $"スコア: {summary.PieceScore}";
            washerTimeValueText.text = $"かどうじかん: {summary.WasherRunningSeconds:0.0}s";
            utilizationValueText.text = $"かどうりつ: {summary.UtilizationRatio * 100f:0.0}%";
            finalScoreValueText.text = $"さいしゅうスコア: {summary.FinalScore:0.000}";
            bestScoreValueText.text = $"ベストスコア: {summary.BestScore:0.000}";

            retryButton.onClick.RemoveAllListeners();
            titleButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(() => onRetry?.Invoke());
            titleButton.onClick.AddListener(() => onTitle?.Invoke());

            SetButtonsInteractable(false);
            rootObject.SetActive(true);
            overlayGroup.blocksRaycasts = true;
            overlayGroup.interactable = true;

            var targets = GetAnimationTargets();
            _animationDriver.ResetVisuals(targets);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelTransform);
            PlayShowAnimationAsync(targets).Forget();
        }

        public void NotifyShowAnimationCompleted()
        {
            SetButtonsInteractable(true);
        }

        private bool ValidateReferences()
        {
            if (rootObject == null ||
                overlayGroup == null ||
                panelTransform == null ||
                headerGroup == null ||
                bodyGroup == null ||
                buttonsGroup == null ||
                titleText == null ||
                pieceScoreValueText == null ||
                washerTimeValueText == null ||
                utilizationValueText == null ||
                finalScoreValueText == null ||
                bestScoreValueText == null ||
                retryButton == null ||
                titleButton == null)
            {
                Debug.LogWarning($"{nameof(GameResultPresenter)} is missing UI references.", this);
                return false;
            }

            if (_animationDriver == null)
            {
                Debug.LogWarning($"{nameof(GameResultPresenter)} is missing an animation driver.", this);
                return false;
            }

            return true;
        }

        private GameResultAnimationTargets GetAnimationTargets()
        {
            return new GameResultAnimationTargets(
                overlayGroup,
                panelTransform,
                headerGroup,
                bodyGroup,
                buttonsGroup,
                finalScoreValueText);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            retryButton.interactable = interactable;
            titleButton.interactable = interactable;
        }

        private async UniTaskVoid PlayShowAnimationAsync(GameResultAnimationTargets targets)
        {
            try
            {
                await _animationDriver.PlayShowAsync(targets);
                NotifyShowAnimationCompleted();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }
}
