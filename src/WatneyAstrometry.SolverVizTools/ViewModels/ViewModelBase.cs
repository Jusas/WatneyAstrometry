// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Controls;
using WatneyAstrometry.SolverVizTools.Abstractions;

namespace WatneyAstrometry.SolverVizTools.ViewModels
{
    public class ViewModelBase : ReactiveObject
    {
        private IWindow _ownerWindow;

        public IWindow OwnerWindow
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
