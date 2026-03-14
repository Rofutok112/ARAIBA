using System.Collections.Generic;
using UnityEngine;

namespace Projects.Scripts.Audio
{
    public sealed class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;

        private readonly Dictionary<string, AudioClip> _clips = new();
        private readonly Dictionary<string, AudioSource> _sources = new();

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

        public static bool Unregister(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            var instance = EnsureInstance();
            instance.StopInternal(key);

            if (instance._sources.Remove(key, out var source) && source != null)
            {
                Destroy(source);
            }

            return instance._clips.Remove(key);
        }

        public static bool IsRegistered(string key)
        {
            return !string.IsNullOrWhiteSpace(key) && EnsureInstance()._clips.ContainsKey(key);
        }

        public static void Play(string key, float volume = 1f, bool loop = false)
        {
            var instance = EnsureInstance();
            if (!instance.TryGetClip(key, out var clip))
            {
                return;
            }

            var source = instance.GetOrCreateSource(key);
            source.Stop();
            source.clip = clip;
            source.volume = volume;
            source.loop = loop;
            source.Play();
        }

        public static void PlayOneShot(string key, float volume = 1f)
        {
            var instance = EnsureInstance();
            if (!instance.TryGetClip(key, out var clip))
            {
                return;
            }

            var source = instance.GetOrCreateSource(key);
            source.clip = clip;
            source.volume = volume;
            source.loop = false;
            source.Play();
        }

        public static void StopAll()
        {
            if (_instance == null)
            {
                return;
            }

            foreach (var source in _instance._sources.Values)
            {
                if (source == null)
                {
                    continue;
                }

                source.Stop();
                source.clip = null;
                source.loop = false;
            }
        }

        public static void Stop(string key)
        {
            if (_instance == null || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            _instance.StopInternal(key);
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
                return _instance;
            }

            var managerObject = new GameObject(nameof(AudioManager));
            DontDestroyOnLoad(managerObject);
            _instance = managerObject.AddComponent<AudioManager>();
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

        private AudioSource GetOrCreateSource(string key)
        {
            if (_sources.TryGetValue(key, out var source) && source != null)
            {
                return source;
            }

            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            _sources[key] = source;
            return source;
        }

        private void StopInternal(string key)
        {
            if (!_sources.TryGetValue(key, out var source) || source == null)
            {
                return;
            }

            source.Stop();
            source.clip = null;
            source.loop = false;
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
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
