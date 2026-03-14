using System.Collections.Generic;
using UnityEngine;

namespace Projects.Scripts.Audio
{
    public sealed class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;

        private readonly Dictionary<string, AudioClip> _clips = new();

        private AudioSource _audioSource;

        /// <summary>
        /// キーとAudioClipを登録する。既に同じキーが登録されている場合は上書きされる。
        /// </summary>
        public static void Register(string key, AudioClip clip)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogWarning("AudioManager.Register failed: key is null or empty.");
                return;
            }

            if (clip == null)
            {
                Debug.LogWarning($"AudioManager.Register failed: clip is null. key={key}");
                return;
            }

            EnsureInstance()._clips[key] = clip;
        }
        
        /// <summary>
        /// キーに対応するAudioClipの登録を解除する。登録されていないキーの場合は何も起こらない。
        /// </summary>
        public static bool Unregister(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return EnsureInstance()._clips.Remove(key);
        }
        
        /// <summary>
        /// キーに対応するAudioClipが登録されているかどうかを返す。
        /// </summary>
        public static bool IsRegistered(string key)
        {
            return !string.IsNullOrWhiteSpace(key) && EnsureInstance()._clips.ContainsKey(key);
        }
        
        /// <summary>
        /// キーに対応するAudioClipを再生する。登録されていないキーの場合は何も起こらない。
        /// </summary>
        public static void Play(string key, float volume = 1f, bool loop = false)
        {
            var instance = EnsureInstance();
            if (!instance.TryGetClip(key, out var clip))
            {
                return;
            }

            instance._audioSource.loop = loop;
            instance._audioSource.clip = clip;
            instance._audioSource.volume = volume;
            instance._audioSource.Play();
        }
        
        /// <summary>
        /// キーに対応するAudioClipを一度だけ再生する。登録されていないキーの場合は何も起こらない。
        /// </summary>
        public static void PlayOneShot(string key, float volumeScale = 1f)
        {
            var instance = EnsureInstance();
            if (!instance.TryGetClip(key, out var clip))
            {
                return;
            }

            instance._audioSource.PlayOneShot(clip, volumeScale);
        }
        
        /// <summary>
        /// 現在再生中のAudioClipを停止する。再生中でない場合は何も起こらない。
        /// </summary>
        public static void Stop()
        {
            if (_instance == null)
            {
                return;
            }

            _instance._audioSource.Stop();
            _instance._audioSource.clip = null;
            _instance._audioSource.loop = false;
        }

        private static AudioManager EnsureInstance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = FindFirstObjectByType<AudioManager>();
            if (_instance != null)
            {
                _instance.Initialize();
                return _instance;
            }

            var managerObject = new GameObject(nameof(AudioManager));
            DontDestroyOnLoad(managerObject);
            _instance = managerObject.AddComponent<AudioManager>();
            _instance.Initialize();
            return _instance;
        }

        private bool TryGetClip(string key, out AudioClip clip)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                clip = null;
                Debug.LogWarning("AudioManager failed: key is null or empty.");
                return false;
            }

            if (_clips.TryGetValue(key, out clip))
            {
                return true;
            }

            Debug.LogWarning($"AudioManager failed: no clip registered for key={key}");
            return false;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Initialize()
        {
            if (_audioSource == null)
            {
                _audioSource = gameObject.GetComponent<AudioSource>();
            }

            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _audioSource.playOnAwake = false;
        }
    }
}
