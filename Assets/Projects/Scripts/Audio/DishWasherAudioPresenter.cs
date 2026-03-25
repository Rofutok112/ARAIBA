using UnityEngine;

namespace Projects.Scripts.Audio
{
    /// <summary>
    /// DishWasher のイベントを受けて SE を再生する。
    /// </summary>
    public class DishWasherAudioPresenter : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioClip washerStartClip;
        [SerializeField] private AudioClip washingNoiseClip;
        [SerializeField] private AudioClip washingCompleteClip;
        [SerializeField, Range(0f, 1f)] private float washerStartVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float washingNoiseVolume = 0.2f;
        [SerializeField, Range(0f, 1f)] private float washingCompleteVolume = 0.7f;

        private string _washingStartKey;
        private string _washingNoiseKey;
        private string _washingCompleteKey;

        private void Awake()
        {
            var keyPrefix = $"{nameof(DishWasherAudioPresenter)}_{GetInstanceID()}";
            _washingStartKey = $"{keyPrefix}_Start";
            _washingNoiseKey = $"{keyPrefix}_Loop";
            _washingCompleteKey = $"{keyPrefix}_Complete";

            RegisterIfAssigned(_washingStartKey, washerStartClip);
            RegisterIfAssigned(_washingNoiseKey, washingNoiseClip);
            RegisterIfAssigned(_washingCompleteKey, washingCompleteClip);
        }

        private void OnDisable()
        {
            AudioManager.Stop(_washingNoiseKey);
        }

        public void HandleWashStarted()
        {
            PlayOneShotIfRegistered(_washingStartKey, washerStartVolume);
            PlayLoopIfRegistered(_washingNoiseKey, washingNoiseVolume);
        }

        public void HandleWashCompleted(float _)
        {
            AudioManager.Stop(_washingNoiseKey);
            PlayOneShotIfRegistered(_washingCompleteKey, washingCompleteVolume);
        }

        private static void RegisterIfAssigned(string key, AudioClip clip)
        {
            if (clip == null) return;
            AudioManager.Register(key, clip);
        }

        private static void PlayOneShotIfRegistered(string key, float volume)
        {
            if (!AudioManager.IsRegistered(key)) return;
            AudioManager.PlayOneShot(key, volume);
        }

        private static void PlayLoopIfRegistered(string key, float volume)
        {
            if (!AudioManager.IsRegistered(key)) return;
            AudioManager.Play(key, volume, true);
        }
    }
}
