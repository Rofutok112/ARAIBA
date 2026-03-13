using Projects.Scripts.Puzzle;
using UnityEngine;
using UnityEngine.UI;

namespace Projects.Scripts.UI
{
    public class PuzzleUI : MonoBehaviour
    {
        [Header("UGUI Buttons")]
        [Tooltip("確定ボタン")]
        [SerializeField]
        private Button confirmButton;

        [Header("Puzzle Components")]
        [SerializeField]
        private GameObject puzzleWindow;

        [SerializeField]
        private PuzzlePieceGenerator puzzlePieceGenerator;

        [Header("Puzzle Window UI")]
        [SerializeField]
        private GameObject puzzleUI;

        private void Awake()
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }

        private void OnConfirmButtonClicked()
        {
            puzzleWindow.SetActive(false);
            puzzleUI.SetActive(false);
            puzzlePieceGenerator.SubmitTray();
        }
    }
}