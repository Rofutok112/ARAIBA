using System;
using Projects.Scripts.Puzzle;
using Projects.Scripts.InteractiveObjects;
using UnityEngine;
using UnityEngine.Events;

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

        [Header("Events")]
        [SerializeField] private UnityEvent<int> onScoreChanged;
        [SerializeField] private UnityEvent<float> onTimeChanged;
        [SerializeField] private UnityEvent onTimeUp;

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

        private void Awake()
        {
            if (dishWasher == null)
            {
                dishWasher = FindFirstObjectByType<DishWasher>();
            }

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
                _hasTimeUpInvoked = true;
                _isTimerRunning = false;
                onTimeUp?.Invoke();
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
            _hasTimeUpInvoked = _remainingTimeSeconds <= 0f;
            if (_hasTimeUpInvoked)
            {
                _isTimerRunning = false;
            }

            RefreshTexts();
        }

        private void RefreshTexts()
        {
            onScoreChanged?.Invoke(CurrentScore);
            onTimeChanged?.Invoke(_remainingTimeSeconds);
        }
    }
}
