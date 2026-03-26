using TMPro;
using UnityEngine;

namespace Projects.Scripts.UI
{
    public readonly struct GameResultAnimationTargets
    {
        public GameResultAnimationTargets(
            CanvasGroup overlayGroup,
            RectTransform panelTransform,
            CanvasGroup headerGroup,
            CanvasGroup bodyGroup,
            CanvasGroup buttonsGroup,
            TMP_Text finalScoreText,
            string finalScoreFormulaText,
            string finalScoreResolvedText)
        {
            OverlayGroup = overlayGroup;
            PanelTransform = panelTransform;
            HeaderGroup = headerGroup;
            BodyGroup = bodyGroup;
            ButtonsGroup = buttonsGroup;
            FinalScoreText = finalScoreText;
            FinalScoreFormulaText = finalScoreFormulaText;
            FinalScoreResolvedText = finalScoreResolvedText;
        }

        public CanvasGroup OverlayGroup { get; }
        public RectTransform PanelTransform { get; }
        public CanvasGroup HeaderGroup { get; }
        public CanvasGroup BodyGroup { get; }
        public CanvasGroup ButtonsGroup { get; }
        public TMP_Text FinalScoreText { get; }
        public string FinalScoreFormulaText { get; }
        public string FinalScoreResolvedText { get; }
    }
}
