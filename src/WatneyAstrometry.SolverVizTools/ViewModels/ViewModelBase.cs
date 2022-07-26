// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Controls;

namespace WatneyAstrometry.SolverVizTools.ViewModels
{
    public class ViewModelBase : ReactiveObject
    {
        private Window _ownerWindow;

        public Window OwnerWindow
        {
            get => _ownerWindow;
            set
            {
                _ownerWindow = value;
                OnViewCreated();
            }
        }

        protected virtual void OnViewCreated()
        {
        }

    }
}
