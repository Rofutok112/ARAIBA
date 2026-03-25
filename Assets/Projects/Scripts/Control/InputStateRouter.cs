using UnityEngine;

namespace Projects.Scripts.Control
{
    public class InputStateRouter : MonoBehaviour
    {
        [SerializeField] private InputManager inputManager;
        [SerializeField] private LayerMask defaultRaycastMask = Physics2D.DefaultRaycastLayers;
        [SerializeField] private LayerMask puzzleRaycastMask;
        [SerializeField] private LayerMask sortingRaycastMask;

        private void Awake()
        {
            if (inputManager == null)
            {
                inputManager = GetComponent<InputManager>();
            }

            if (defaultRaycastMask.value == 0) defaultRaycastMask = Physics2D.DefaultRaycastLayers;
            if (puzzleRaycastMask.value == 0) puzzleRaycastMask = InputLayerUtility.GetMask(InputTargetRole.Puzzle, defaultRaycastMask);
            if (sortingRaycastMask.value == 0) sortingRaycastMask = InputLayerUtility.GetMask(InputTargetRole.Sorting, defaultRaycastMask);
        }

        public void SetOperationState(InputOperationState operationState)
        {
            if (inputManager == null) return;

            inputManager.SetRaycastMaskOverride(GetMask(operationState.ToTargetRole()));
        }

        public void ResetToDefault()
        {
            if (inputManager == null) return;

            inputManager.SetRaycastMaskOverride(defaultRaycastMask);
        }

        private LayerMask GetMask(InputTargetRole role)
        {
            return role switch
            {
                InputTargetRole.Puzzle => puzzleRaycastMask,
                InputTargetRole.Sorting => sortingRaycastMask,
                _ => defaultRaycastMask,
            };
        }
    }
}
