using System;
using Projects.Scripts.InteractiveObjects;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Projects.Scripts.UI
{
    public class PuzzleUI : MonoBehaviour
    {
        [Header("UGUI Buttons")]
        [Tooltip("確定ボタン")]
        [SerializeField] private Button confirmButton;

        [Header("Puzzle Components")]
        [SerializeField] private GameObject puzzleWindow;
        [SerializeField] private DishWasher dishWasher;

        [Header("Puzzle Window UI")]
        [SerializeField] private GameObject puzzleUI;
        [SerializeField] private Vector3 closeRiseOffset = new(0f, 0.15f, 0f);
        [SerializeField] private Vector3 closeFallOffset = new(0f, -0.5f, 0f);
        [SerializeField] private float closeRiseDuration = 0.12f;
        [SerializeField] private float closeFallDuration = 0.25f;

        [Header("Washer Timer UI")]
        [SerializeField] private Image washerTimerFillImage;
        [SerializeField, Range(0f, 1f)] private float warningThreshold = 0.3f;
        [SerializeField] private Color normalTimerColor = new(0.27f, 0.78f, 0.98f, 1f);
        [SerializeField] private Color warningTimerColor = new(1f, 0.25f, 0.25f, 1f);
        [SerializeField, Min(0.05f)] private float warningColorPulseDuration = 0.35f;

        private Vector3 puzzleWindowDefaultPosition;
        private Tween puzzleWindowTween;
        private CancellationTokenSource closeAnimationCts;

        private void Awake()
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            if (puzzleWindow != null)
            {
                puzzleWindowDefaultPosition = puzzleWindow.transform.position;
            }

            UpdateWasherTimerVisual(0f);
        }

        private void OnEnable()
        {
            if (dishWasher == null) return;

            dishWasher.OnWashProgressChanged += HandleWashProgressChanged;
            dishWasher.OnWashStateChanged += HandleWashStateChanged;
            HandleWashProgressChanged(dishWasher.CurrentNormalizedRemainingTime);
            HandleWashStateChanged(dishWasher.IsRunning);
        }

        private void OnDisable()
        {
            puzzleWindowTween?.Kill();
            closeAnimationCts?.Cancel();
            closeAnimationCts?.Dispose();
            closeAnimationCts = null;

            if (dishWasher != null)
            {
                dishWasher.OnWashProgressChanged -= HandleWashProgressChanged;
                dishWasher.OnWashStateChanged -= HandleWashStateChanged;
            }
        }

        private void OnConfirmButtonClicked()
        {
            if (puzzleWindow == null)
            {
                puzzleUI.SetActive(false);
                return;
            }

            closeAnimationCts?.Cancel();
            closeAnimationCts?.Dispose();
            closeAnimationCts = new CancellationTokenSource();
            ClosePuzzleWindowAsync(closeAnimationCts.Token).Forget();
        }

        private async UniTaskVoid ClosePuzzleWindowAsync(CancellationToken cancellationToken)
        {
            puzzleWindowTween?.Kill();

            var currentPosition = puzzleWindow.transform.position;
            var riseTarget = currentPosition + closeRiseOffset;
            var fallTarget = puzzleWindowDefaultPosition + closeFallOffset;

            try
            {
                puzzleWindowTween = puzzleWindow.transform.DOMove(riseTarget, closeRiseDuration).SetEase(Ease.OutQuad);
                await puzzleWindowTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(cancellationToken);

                puzzleWindowTween = puzzleWindow.transform.DOMove(fallTarget, closeFallDuration).SetEase(Ease.InBack);
                await puzzleWindowTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(cancellationToken);

                puzzleWindow.transform.position = puzzleWindowDefaultPosition;
                puzzleWindow.SetActive(false);
                puzzleUI.SetActive(false);
            }
            catch (OperationCanceledException)
            {
                puzzleWindowTween?.Kill();
            }
            finally
            {
                puzzleWindowTween = null;

                if (closeAnimationCts != null && closeAnimationCts.Token == cancellationToken)
                {
                    closeAnimationCts.Dispose();
                    closeAnimationCts = null;
                }
            }
        }

        private void HandleWashProgressChanged(float normalizedRemainingTime)
        {
            UpdateWasherTimerVisual(normalizedRemainingTime);
        }

        private void HandleWashStateChanged(bool isRunning)
        {
            if (!isRunning)
            {
                UpdateWasherTimerVisual(0f);
            }
        }

        private void Update()
        {
            if (dishWasher == null || !dishWasher.IsRunning) return;
            if (dishWasher.CurrentNormalizedRemainingTime > warningThreshold) return;

            UpdateWasherTimerVisual(dishWasher.CurrentNormalizedRemainingTime);
        }

        private void UpdateWasherTimerVisual(float normalizedRemainingTime)
        {
            if (washerTimerFillImage == null) return;

            var clampedValue = Mathf.Clamp01(normalizedRemainingTime);
            washerTimerFillImage.fillAmount = clampedValue;

            if (clampedValue > warningThreshold || warningThreshold <= 0f)
            {
                washerTimerFillImage.color = normalTimerColor;
                return;
            }

            var warningProgress = 1f - (clampedValue / warningThreshold);
            var baseWarningColor = Color.Lerp(normalTimerColor, warningTimerColor, warningProgress);
            var pulseT = Mathf.PingPong(Time.unscaledTime / warningColorPulseDuration, 1f);
            washerTimerFillImage.color = Color.Lerp(baseWarningColor, warningTimerColor, pulseT);
        }
    }
}
