using System;
using Cysharp.Threading.Tasks;
using Projects.Scripts.Audio;
using Projects.Scripts.BackGround;
using Projects.Scripts.Control;
using UnityEngine;
using UnityEngine.Events;

namespace Projects.Scripts.InteractiveObjects
{
    public enum DishWasherState
    {
        Idle,
        Running,
        Done,
    }

    public class DishWasher : MonoBehaviour, IInputHandler
    {
        [Header("Settings")]
        [SerializeField, Min(1f)] private float washDuration = 10f;

        [Header("References")]
        [SerializeField] private RackManager rackManager;
        [SerializeField] private WasherAnim washerAnim;

        [Header("Audio")]
        [SerializeField] private AudioClip washerStartClip;
        [SerializeField] private AudioClip washingNoiseClip;
        [SerializeField] private AudioClip washingCompleteClip;

        [Header("Events")]
        [SerializeField] private UnityEvent<float> onWashProgressChanged;
        [SerializeField] private UnityEvent<DishWasherState> onStateChanged;

        private DishWasherState _state = DishWasherState.Idle;
        private Rack _currentRack;

        public DishWasherState State => _state;
        public Rack CurrentRack => _currentRack;

        private void Start()
        {
            AudioManager.Register("WashingNoise", washingNoiseClip);
            AudioManager.Register("WashingStart", washerStartClip);
            AudioManager.Register("WashingComplete", washingCompleteClip);
        }

        public void OnInputBegin(Vector2 pos)
        {
            switch (_state)
            {
                case DishWasherState.Idle:
                    TryStartWashing();
                    break;
                case DishWasherState.Running:
                    break;
                case DishWasherState.Done:
                    TakeOutRack();
                    break;
            }
        }

        /// <summary>
        /// Packed状態のラックを探して洗浄を開始する
        /// </summary>
        private void TryStartWashing()
        {
            var rack = rackManager.FindPackedRack();
            if (rack == null) return;

            _currentRack = rack;
            rack.SetState(RackState.Washing);
            rack.gameObject.SetActive(false);

            SetState(DishWasherState.Running);
            RunWashTimer().Forget();
        }

        /// <summary>
        /// 洗浄完了後にラックを取り出す
        /// </summary>
        private void TakeOutRack()
        {
            if (_currentRack == null) return;

            _currentRack.SetState(RackState.Washed);
            _currentRack.gameObject.SetActive(true);
            _currentRack = null;

            SetState(DishWasherState.Idle);

            // 次のPackedラックがあれば即座に洗浄開始
            TryStartWashing();
        }

        private async UniTaskVoid RunWashTimer()
        {
            AudioManager.PlayOneShot("WashingStart", volume: 0.7f);
            AudioManager.Play("WashingNoise", volume: 0.2f, loop: true);
            washerAnim?.StartVibration();

            var remaining = washDuration;
            while (remaining > 0f)
            {
                await UniTask.Yield();
                remaining -= Time.deltaTime;
                var normalized = Mathf.Clamp01(remaining / washDuration);
                onWashProgressChanged?.Invoke(normalized);
            }

            washerAnim?.StopVibration();
            AudioManager.Stop("WashingNoise");
            AudioManager.PlayOneShot("WashingComplete", volume: 0.7f);

            onWashProgressChanged?.Invoke(0f);
            SetState(DishWasherState.Done);
        }

        private void SetState(DishWasherState newState)
        {
            _state = newState;
            onStateChanged?.Invoke(newState);
        }

        public void OnInputDrag(Vector2 pos) { }
        public void OnInputEnd(Vector2 pos) { }
    }
}
