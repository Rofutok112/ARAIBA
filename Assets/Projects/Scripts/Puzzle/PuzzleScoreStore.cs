using UnityEngine;

namespace Projects.Scripts.Puzzle
{
    /// <summary>
    /// パズルのスコアを永続化する
    /// </summary>
    public static class PuzzleScoreStore
    {
        private const string LatestScoreKey = "Puzzle.LatestScore";
        private const string BestScoreKey = "Puzzle.BestScore";

        public static float LatestScore => PlayerPrefs.GetFloat(LatestScoreKey, 0f);
        public static float BestScore => PlayerPrefs.GetFloat(BestScoreKey, 0f);

        public static void SaveScore(float score)
        {
            var clampedScore = Mathf.Max(0f, score);

            PlayerPrefs.SetFloat(LatestScoreKey, clampedScore);

            if (clampedScore > BestScore)
            {
                PlayerPrefs.SetFloat(BestScoreKey, clampedScore);
            }

            PlayerPrefs.Save();
        }

        public static float AddScore(float delta)
        {
            var nextScore = LatestScore + Mathf.Max(0f, delta);
            SaveScore(nextScore);
            return nextScore;
        }
    }
}
