using System;
using Projects.Scripts.Puzzle;
using Projects.Scripts.InteractiveObjects;
using UnityEngine;

namespace Projects.Scripts.UI
{
    /// <summary>
    /// ゲーム全体のスコアと食洗機稼働率を管理する。
    /// 最終スコア = ピース素点合計 * (食洗機総稼働時間 / ゲーム制限時間)
    /// </summary>
    public class GameScoreManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField, Min(1f)] private float gameTimeLimitSeconds = 60f;
        [SerializeField] private bool startTimerOnAwake = true;

        [Header("References")]
        [SerializeField] private DishWasher dishWasher;

        private int _totalPiecePoints;
        private float _remainingTimeSeconds;
        private bool _isTimerRunning;
        private bool _hasTimeUpInvoked;

        public int TotalPiecePoints => _totalPiecePoints;
        public float TotalWasherRunningSeconds => dishWasher != null ? dishWasher.TotalRunningSeconds : 0f;
        public float GameTimeLimitSeconds => gameTimeLimitSeconds;
        public float UtilizationRatio => TotalWasherRunningSeconds / Mathf.Max(1f, gameTimeLimitSeconds);
        public int CurrentScore => _totalPiecePoints;
        public float FinalResultScore => _totalPiecePoints * UtilizationRatio;
        public float RemainingTimeSeconds => _remainingTimeSeconds;
        public bool IsTimerRunning => _isTimerRunning;
        public bool IsTimeUp => _hasTimeUpInvoked;

        public event Action<int> ScoreChanged;
        public event Action<float> TimeChanged;
        public event Action TimeUp;

        private void Awake()
        {
            _remainingTimeSeconds = gameTimeLimitSeconds;
            _isTimerRunning = startTimerOnAwake;
            RefreshTexts();
            PuzzleScoreStore.SaveScore(0f);
        }

        private void Update()
        {
            if (!_isTimerRunning || _remainingTimeSeconds <= 0f) return;

            _remainingTimeSeconds = Mathf.Max(0f, _remainingTimeSeconds - Time.deltaTime);
            RefreshTexts();

            if (_remainingTimeSeconds <= 0f && !_hasTimeUpInvoked)
            {
                HandleTimeUp();
            }
        }

        public void ResetSessionScore()
        {
            _totalPiecePoints = 0;
            _remainingTimeSeconds = gameTimeLimitSeconds;
            _hasTimeUpInvoked = false;
            _isTimerRunning = startTimerOnAwake;
            RefreshTexts();
            PuzzleScoreStore.SaveScore(0f);
        }

        public void AddSortedPiecePoints(int piecePoints)
        {
            if (_hasTimeUpInvoked)
            {
                return;
            }

            _totalPiecePoints += Mathf.Max(0, piecePoints);
            RefreshTexts();
            PuzzleScoreStore.SaveScore(CurrentScore);
        }
        
        public void StartTimer()
        {
            if (_remainingTimeSeconds <= 0f) return;
            _isTimerRunning = true;
        }

        public void PauseTimer()
        {
            _isTimerRunning = false;
        }

        public void ResumeTimer()
        {
            StartTimer();
        }

        public void SetRemainingTime(float seconds)
        {
            _remainingTimeSeconds = Mathf.Clamp(seconds, 0f, gameTimeLimitSeconds);
            if (_remainingTimeSeconds <= 0f)
            {
                HandleTimeUp();
            }
            else
            {
                _hasTimeUpInvoked = false;
            }

            RefreshTexts();
        }

        public GameResultSummary BuildResultSummary()
        {
            return new GameResultSummary(
                _totalPiecePoints,
                TotalWasherRunningSeconds,
                UtilizationRatio,
                FinalResultScore,
                PuzzleScoreStore.BestScore);
        }

        private void RefreshTexts()
        {
            ScoreChanged?.Invoke(CurrentScore);
            TimeChanged?.Invoke(_remainingTimeSeconds);
        }

        private void HandleTimeUp()
        {
            if (_hasTimeUpInvoked)
            {
                return;
            }

            _remainingTimeSeconds = 0f;
            _hasTimeUpInvoked = true;
            _isTimerRunning = false;
            PuzzleScoreStore.SaveScore(FinalResultScore);
            TimeUp?.Invoke();
        }
    }
}
