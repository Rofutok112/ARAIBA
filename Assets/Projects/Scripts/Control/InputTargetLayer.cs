using UnityEngine;

namespace Projects.Scripts.Control
{
    public class InputTargetLayer : MonoBehaviour
    {
        [SerializeField] private InputTargetRole role = InputTargetRole.Default;
        [SerializeField] private bool applyToChildren = true;

        public InputTargetRole Role => role;

        private void Awake()
        {
            Apply();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Apply();
        }
#endif

        public void Apply()
        {
            if (applyToChildren)
            {
                InputLayerUtility.ApplyRoleRecursively(gameObject, role);
                return;
            }

            var layer = InputLayerUtility.GetLayer(role);
            if (layer >= 0)
            {
                gameObject.layer = layer;
            }
        }

        public void SetRole(InputTargetRole newRole)
        {
            role = newRole;
            Apply();
        }
    }
}
