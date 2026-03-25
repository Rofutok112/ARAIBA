using TMPro;
using UnityEngine;

namespace Projects.Scripts.UI
{
    /// <summary>
    /// HUD の Score / Time 表示を更新する。
    /// UnityEvent から各 setter を呼ぶ前提の受け口。
    /// </summary>
    public class GameHudTextPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text timeText;

        [Header("Format")]
        [SerializeField] private string scorePrefix = "Score: ";
        [SerializeField] private string timePrefix = "Time: ";
        [SerializeField] private string timeSuffix = "s";

        private void Awake()
        {
            RefreshScore(0);
        }

        public void RefreshScore(int score)
        {
            if (scoreText == null) return;
            scoreText.text = $"{scorePrefix}{Mathf.Max(0, score)}";
        }

        public void RefreshTime(float seconds)
        {
            if (timeText == null) return;

            var clampedSeconds = Mathf.Max(0f, seconds);
            timeText.text = $"{timePrefix}{clampedSeconds:0.0}{timeSuffix}";
        }

        public void RefreshTimeFromInt(int seconds)
        {
            RefreshTime(seconds);
        }

        public void ClearTime()
        {
            if (timeText == null) return;
            timeText.text = string.Empty;
        }
    }
}
