using Cysharp.Threading.Tasks;
using Projects.Scripts.InteractiveObjects;
using Projects.Scripts.Puzzle;
using Projects.Scripts.Sorting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Projects.Scripts.UI
{
    public class GameTutorialController : MonoBehaviour
    {
        private enum TutorialStep
        {
            None,
            TapEmptyRack,
            PlacePiece,
            ConfirmPuzzle,
            TapDishWasher,
            UnloadRack,
            WaitForWashedRack,
            TapWashedRack,
            SortingPrompt,
            FinalLoopHint,
            Completed,
        }

        [Header("References")]
        [SerializeField] private TutorialOverlayPresenter overlayPresenter;
        [SerializeField] private RackManager rackManager;
        [SerializeField] private PuzzlePieceGenerator puzzlePieceGenerator;
        [SerializeField] private DishWasher dishWasher;
        [SerializeField] private SortingManager sortingManager;

        [Header("Focus Targets")]
        [Header("Messages")]
        [SerializeField] private string emptyRackMessage = "空のラックをタップしてみよう";
        [SerializeField] private string placePieceMessage = "ピースを1つ置いてみよう";
        [SerializeField] private string confirmMessage = "置けたら Confirm を押そう";
        [SerializeField] private string dishWasherMessage = "食洗機をタップして洗浄開始！";
        [SerializeField] private string unloadRackMessage = "ラックをとりだそう！";
        [SerializeField] private string washedRackMessage = "洗い終わったラックをタップしよう";
        [SerializeField] private string sortingMessage = "選別しよう！";
        [SerializeField] private string finalLoopHintMessage = "パズルとせんべつをすばやくくりかえして しょくせんきを まわしつづけよう";
        [SerializeField, Min(0f)] private float finalMessageDuration = 3f;
        [SerializeField] private string titleSceneName = "Title";

        private TutorialStep _currentStep;
        private bool _isEndingTutorial;

        private void Awake()
        {
            ValidateReferences();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void Start()
        {
            BeginTutorialAsync().Forget();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            overlayPresenter?.HideImmediate();
        }

        private async UniTaskVoid BeginTutorialAsync()
        {
            if (!ValidateReferences())
            {
                return;
            }

            await UniTask.NextFrame();
            ShowStep(TutorialStep.TapEmptyRack);
        }

        private void ShowStep(TutorialStep step)
        {
            _currentStep = step;

            switch (step)
            {
                case TutorialStep.TapEmptyRack:
                    overlayPresenter.Show(emptyRackMessage, true);
                    break;
                case TutorialStep.PlacePiece:
                    overlayPresenter.Show(placePieceMessage, true);
                    break;
                case TutorialStep.ConfirmPuzzle:
                    overlayPresenter.Show(confirmMessage, true);
                    break;
                case TutorialStep.TapDishWasher:
                    overlayPresenter.Show(dishWasherMessage, true);
                    break;
                case TutorialStep.UnloadRack:
                    overlayPresenter.Show(unloadRackMessage, true);
                    break;
                case TutorialStep.TapWashedRack:
                    overlayPresenter.Show(washedRackMessage, true);
                    break;
                case TutorialStep.SortingPrompt:
                    overlayPresenter.Show(sortingMessage, false);
                    break;
                case TutorialStep.FinalLoopHint:
                    ShowFinalLoopHintAsync().Forget();
                    break;
                case TutorialStep.Completed:
                    overlayPresenter.Hide();
                    break;
            }
        }

        private async UniTaskVoid ShowFinalLoopHintAsync()
        {
            if (_isEndingTutorial)
            {
                return;
            }

            _currentStep = TutorialStep.FinalLoopHint;
            _isEndingTutorial = true;
            overlayPresenter.Show(finalLoopHintMessage, false);
            Canvas.ForceUpdateCanvases();
            await UniTask.NextFrame();
            await UniTask.Delay(
                System.TimeSpan.FromSeconds(Mathf.Max(1f, finalMessageDuration)),
                ignoreTimeScale: true);

            if (this == null || _currentStep != TutorialStep.FinalLoopHint)
            {
                return;
            }

            ShowStep(TutorialStep.Completed);
            LoadTitleScene();
        }

        private void HandlePuzzleOpened(Rack rack)
        {
            if (_currentStep == TutorialStep.TapEmptyRack)
            {
                ShowStep(TutorialStep.PlacePiece);
            }
        }

        private void HandlePiecePlaced(PuzzlePiece piece)
        {
            if (_currentStep == TutorialStep.PlacePiece)
            {
                ShowStep(TutorialStep.ConfirmPuzzle);
            }
        }

        private void HandlePuzzleConfirmed()
        {
            if (_currentStep == TutorialStep.ConfirmPuzzle)
            {
                ShowStep(TutorialStep.TapDishWasher);
            }
        }

        private void HandleWashStarted()
        {
            if (_currentStep == TutorialStep.TapDishWasher)
            {
                _currentStep = TutorialStep.WaitForWashedRack;
                overlayPresenter.Hide();
            }
        }

        private void HandleWashCompleted(float elapsedSeconds)
        {
            if (_currentStep == TutorialStep.WaitForWashedRack)
            {
                ShowStep(TutorialStep.UnloadRack);
            }
        }

        private void HandleRackUnloaded(Rack rack)
        {
            if (_currentStep == TutorialStep.WaitForWashedRack || _currentStep == TutorialStep.UnloadRack)
            {
                ShowStep(TutorialStep.TapWashedRack);
            }
        }

        private void HandleSortingStarted(Rack rack)
        {
            if (_currentStep == TutorialStep.TapWashedRack)
            {
                ShowStep(TutorialStep.SortingPrompt);
            }
        }

        private void HandleSortingCompleted()
        {
            if (_currentStep == TutorialStep.SortingPrompt)
            {
                ShowStep(TutorialStep.FinalLoopHint);
            }
        }

        private void SubscribeEvents()
        {
            if (rackManager != null)
            {
                rackManager.PuzzleOpened += HandlePuzzleOpened;
                rackManager.PuzzleConfirmed += HandlePuzzleConfirmed;
            }

            if (puzzlePieceGenerator != null)
            {
                puzzlePieceGenerator.OnPiecePlacedOnGrid += HandlePiecePlaced;
            }

            if (dishWasher != null)
            {
                dishWasher.WashStarted += HandleWashStarted;
                dishWasher.WashCompleted += HandleWashCompleted;
                dishWasher.RackUnloaded += HandleRackUnloaded;
            }

            if (sortingManager != null)
            {
                sortingManager.SortingStarted += HandleSortingStarted;
                sortingManager.SortingCompleted += HandleSortingCompleted;
            }
        }

        private void UnsubscribeEvents()
        {
            if (rackManager != null)
            {
                rackManager.PuzzleOpened -= HandlePuzzleOpened;
                rackManager.PuzzleConfirmed -= HandlePuzzleConfirmed;
            }

            if (puzzlePieceGenerator != null)
            {
                puzzlePieceGenerator.OnPiecePlacedOnGrid -= HandlePiecePlaced;
            }

            if (dishWasher != null)
            {
                dishWasher.WashStarted -= HandleWashStarted;
                dishWasher.WashCompleted -= HandleWashCompleted;
                dishWasher.RackUnloaded -= HandleRackUnloaded;
            }

            if (sortingManager != null)
            {
                sortingManager.SortingStarted -= HandleSortingStarted;
                sortingManager.SortingCompleted -= HandleSortingCompleted;
            }
        }

        private void LoadTitleScene()
        {
            if (!string.IsNullOrWhiteSpace(titleSceneName))
            {
                SceneManager.LoadScene(titleSceneName);
            }
        }

        private bool ValidateReferences()
        {
            if (overlayPresenter == null ||
                rackManager == null ||
                puzzlePieceGenerator == null ||
                dishWasher == null ||
                sortingManager == null)
            {
                Debug.LogWarning($"{nameof(GameTutorialController)} is missing scene references.", this);
                return false;
            }

            return true;
        }
    }
}
