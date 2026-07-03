
using System;

namespace ReplicaProjects.MagicSort
{
    public struct SelectionResult
    {
        public int SourceIndex;
        public int TargetIndex;
    }

    public class SelectionSystem
    {
        private enum SelectionAction
        {
            None,
            SourceSelected,
            ReTargetted,
            DeSelected,
            Consumed
        }

        public Action<int> FirstItemSelected;
        public Action<SelectionResult> SelectionCompleted;

        private SelectionAction _currentAction;

        public void OnClicked(int selectionIndex)
        {

        }
    }
}
