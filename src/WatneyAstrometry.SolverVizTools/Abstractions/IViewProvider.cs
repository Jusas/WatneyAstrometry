// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using WatneyAstrometry.SolverVizTools.ViewModels;

namespace WatneyAstrometry.SolverVizTools.Abstractions
{
    public interface IViewProvider
    {
        void Register<TView, TViewModel>()
            where TView : IControl
            where TViewModel : ViewModelBase;

        IWindow Instantiate<TViewModel>(TViewModel usingViewModel = null) 
            where TViewModel : ViewModelBase;
    }
}
