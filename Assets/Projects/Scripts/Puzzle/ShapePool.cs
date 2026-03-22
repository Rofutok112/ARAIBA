using System.Collections.Generic;
using UnityEngine;

namespace Projects.Scripts.Puzzle
{
    [CreateAssetMenu(fileName = "NewShapePool", menuName = "Puzzle/Shape Pool")]
    public class ShapePool : ScriptableObject
    {
        [SerializeField] private PuzzlePieceShape[] shapes;

        public IReadOnlyList<PuzzlePieceShape> Shapes => shapes;
    }
}
