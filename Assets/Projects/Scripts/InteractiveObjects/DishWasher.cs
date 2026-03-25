using System;
using Cysharp.Threading.Tasks;
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
        ReadyToUnload,
    }

    public class DishWasher : MonoBehaviour, IInputHandler, IInteractionHintTarget
    {
        [Header("Settings")]
        [SerializeField, Min(1f)] private float washDuration = 10f;

        [Header("References")]
        [SerializeField] private RackManager rackManager;
        [SerializeField] private WasherAnim washerAnim;

        [Header("Events")]
        [SerializeField] private UnityEvent onWashStarted;
        [SerializeField] private UnityEvent<float> onWashProgressChanged;
        [SerializeField] private UnityEvent<float> onWashCompleted;
        [SerializeField] private UnityEvent<DishWasherState> onStateChanged;

        private DishWasherState _state = DishWasherState.Idle;
        private Rack _currentRack;
        private float _completedWashElapsedSeconds;
        private float _totalRunningSeconds;
        private SpriteRenderer _spriteRenderer;

        public DishWasherState State => _state;
        public Rack CurrentRack => _currentRack;
        public float TotalRunningSeconds => _totalRunningSeconds;
        public bool ShouldShowInteractionHint => HasRackReadyToWash() || HasRackReadyToUnload();
        public SpriteRenderer HintSpriteRenderer => _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
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
                case DishWasherState.ReadyToUnload:
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

            var rack = _currentRack;
            rack.SetState(RackState.Washed);
            rack.gameObject.SetActive(true);
            _currentRack = null;
            _completedWashElapsedSeconds = 0f;

            SetState(DishWasherState.Idle);
        }

        private async UniTaskVoid RunWashTimer()
        {
            onWashStarted?.Invoke();
            washerAnim?.StartVibration();

            var remaining = washDuration;
            var elapsed = 0f;
            while (remaining > 0f)
            {
                await UniTask.Yield();
                elapsed += Time.deltaTime;
                remaining -= Time.deltaTime;
                var normalized = Mathf.Clamp01(remaining / washDuration);
                onWashProgressChanged?.Invoke(normalized);
            }

            washerAnim?.StopVibration();

            onWashProgressChanged?.Invoke(0f);
            _completedWashElapsedSeconds = Mathf.Min(elapsed, washDuration);
            _totalRunningSeconds += _completedWashElapsedSeconds;
            onWashCompleted?.Invoke(_completedWashElapsedSeconds);
            SetState(DishWasherState.ReadyToUnload);
        }

        private void SetState(DishWasherState newState)
        {
            _state = newState;
            onStateChanged?.Invoke(newState);
        }

        private bool HasRackReadyToWash()
        {
            return _state == DishWasherState.Idle && rackManager != null && rackManager.FindPackedRack() != null;
        }

        private bool HasRackReadyToUnload()
        {
            return _state == DishWasherState.ReadyToUnload && _currentRack != null;
        }

        public void OnInputDrag(Vector2 pos) { }
        public void OnInputEnd(Vector2 pos) { }
    }
}
