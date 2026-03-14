using Cysharp.Threading.Tasks;
using Projects.Scripts.Audio;
using Projects.Scripts.BackGround;
using Projects.Scripts.Control;
using UnityEngine;

namespace Projects.Scripts.InteractiveObjects
{
    /// <summary>
    /// 食洗機のGameObject
    /// </summary>
    public class DishWasher : MonoBehaviour, IInputHandler
    {
        private const float WashingTime = 5.0f;

        [SerializeField] private WasherAnim washerAnim;
        [SerializeField] private AudioClip washerStartClip;
        [SerializeField] private AudioClip washingNoiseClip;
        [SerializeField] private AudioClip washingCompleteClip;

        private bool _isRunning;

        private void Start()
        {
            AudioManager.Register("WashingNoise", washingNoiseClip);
            AudioManager.Register("WashingStart", washerStartClip);
            AudioManager.Register("WashingComplete", washingCompleteClip);
        }

        public void OnInputBegin(Vector2 pos)
        {
            if (_isRunning) return;
            WasherTimer().Forget();
        }

        private async UniTaskVoid WasherTimer()
        {
            _isRunning = true;
            washerAnim?.StartVibration();
            AudioManager.PlayOneShot("WashingStart", volume: 0.7f);
            AudioManager.Play("WashingNoise", volume: 0.2f, loop: true);

            var currentTime = WashingTime;
            while (currentTime >= 0)
            {
                await UniTask.Yield();
                currentTime -= Time.deltaTime;
            }

            washerAnim?.StopVibration();
            AudioManager.Stop("WashingNoise");
            AudioManager.PlayOneShot("WashingComplete", volume: 0.7f);
            _isRunning = false;
            Debug.Log("洗浄完了！");
        }

        public void OnInputDrag(Vector2 pos) { }

        public void OnInputEnd(Vector2 pos) { }
    }
}
