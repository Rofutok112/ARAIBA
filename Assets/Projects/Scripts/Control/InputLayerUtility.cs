using UnityEngine;

namespace Projects.Scripts.Control
{
    public static class InputLayerUtility
    {
        private const string PuzzleInputLayerName = "PuzzleInput";
        private const string SortingInputLayerName = "SortingInput";

        public static LayerMask GetMask(InputTargetRole role, LayerMask fallbackMask)
        {
            return role switch
            {
                InputTargetRole.Puzzle => ResolveSingleLayerMask(PuzzleInputLayerName, fallbackMask),
                InputTargetRole.Sorting => ResolveSingleLayerMask(SortingInputLayerName, fallbackMask),
                _ => fallbackMask,
            };
        }

        public static int GetLayer(InputTargetRole role)
        {
            return role switch
            {
                InputTargetRole.Puzzle => LayerMask.NameToLayer(PuzzleInputLayerName),
                InputTargetRole.Sorting => LayerMask.NameToLayer(SortingInputLayerName),
                _ => 0,
            };
        }

        public static void ApplyRoleRecursively(GameObject obj, InputTargetRole role)
        {
            if (obj == null) return;

            var layer = GetLayer(role);
            if (layer < 0) return;

            ApplyLayerRecursively(obj, layer);
        }

        private static LayerMask ResolveSingleLayerMask(string layerName, LayerMask fallbackMask)
        {
            var layerIndex = LayerMask.NameToLayer(layerName);
            return layerIndex >= 0 ? (LayerMask)(1 << layerIndex) : fallbackMask;
        }

        private static void ApplyLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                if (child == null) continue;
                ApplyLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
