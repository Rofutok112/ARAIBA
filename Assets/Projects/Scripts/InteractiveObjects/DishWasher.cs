using Cysharp.Threading.Tasks;
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

        private bool _isRunning;

        public void OnInputBegin(Vector2 pos)
        {
            if (_isRunning) return;
            WasherTimer().Forget();
        }

        private async UniTaskVoid WasherTimer()
        {
            _isRunning = true;
            washerAnim?.StartVibration();

            var currentTime = WashingTime;
            while (currentTime >= 0)
            {
                await UniTask.Yield();
                currentTime -= Time.deltaTime;
            }

            washerAnim?.StopVibration();
            _isRunning = false;
            Debug.Log("洗浄完了！");
        }

        public void OnInputDrag(Vector2 pos) { }

        public void OnInputEnd(Vector2 pos) { }
    }
}
