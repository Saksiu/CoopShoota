
using System.Linq;

namespace AIUtils
{
    public class StateMachine
    {
        public StateMachineState CurrentState;

        public void Execute()
        {
            foreach (var transition in CurrentState.transitions.Keys.Where(transition => transition.Invoke()))
            {
                CurrentState.ExitAction?.Invoke();
                    
                CurrentState = CurrentState.transitions[transition];
                    
                CurrentState.StartAction?.Invoke();
            }

            CurrentState.UpdateAction?.Invoke();
        }
    }
}

