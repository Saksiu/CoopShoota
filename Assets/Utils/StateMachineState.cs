using System;
using System.Collections.Generic;

namespace AIUtils
{
    public class StateMachineState
    {
        public Action StartAction;

        public Action UpdateAction;

        public Action ExitAction;

        //internal readonly List<(Func<bool>, StateMachineState)> transitions = new List<(Func<bool>, StateMachineState)>();
        internal readonly Dictionary<Func<bool>, StateMachineState> transitions = new();
        private readonly string _name;
        public override string ToString()=>_name;

        public void AddTransition(Func<bool> func, StateMachineState state)
        {
            transitions.Add(func, state);
        }

        /*public StateMachineState(Action start, Action update, Action exit)
        {
            StartAction = start;
            UpdateAction = update;
            ExitAction = exit;
            _name = this.;
        }*/
        public StateMachineState(string name, Action start, Action update, Action exit)
        {
            _name = name;
            StartAction = start;
            UpdateAction = update;
            ExitAction = exit;
        }
    }
}