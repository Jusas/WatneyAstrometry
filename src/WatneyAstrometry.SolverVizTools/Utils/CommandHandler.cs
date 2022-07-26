// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WatneyAstrometry.SolverVizTools.Utils
{
    /// <summary>
    /// A very simple command handler.
    /// </summary>
    public class CommandHandler : ICommand
    {

        private Action<object> _commandAction;
        private Func<object, bool> _canExecuteAction;

        public CommandHandler(Action commandAction)
        {
            _commandAction = o => commandAction();
            _canExecuteAction = o => true;
        }

        public CommandHandler(Action<object> commandAction)
        {
            _commandAction = commandAction;
            _canExecuteAction = o => true;
        }

        public CommandHandler(Action commandAction, Func<object, bool> canExecute)
        {
            _commandAction = o => commandAction();
            _canExecuteAction = canExecute;
        }

        public CommandHandler(Action<object> commandAction, Func<object, bool> canExecute)
        {
            _commandAction = commandAction;
            _canExecuteAction = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecuteAction(parameter);
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _commandAction(parameter);
        }
    }
}
