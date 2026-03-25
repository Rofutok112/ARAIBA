namespace Projects.Scripts.Control
{
    public enum InputOperationState
    {
        Default,
        Puzzle,
        Sorting,
    }

    public static class InputOperationStateExtensions
    {
        public static InputTargetRole ToTargetRole(this InputOperationState state)
        {
            return state switch
            {
                InputOperationState.Puzzle => InputTargetRole.Puzzle,
                InputOperationState.Sorting => InputTargetRole.Sorting,
                _ => InputTargetRole.Default,
            };
        }
    }
}
