using UnityEngine;

namespace Projects.Scripts.Control
{
    public interface IInteractionHintTarget
    {
        bool ShouldShowInteractionHint { get; }
        SpriteRenderer HintSpriteRenderer { get; }
    }
}
