﻿// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Splat;
using WatneyAstrometry.SolverVizTools.Abstractions;
using WatneyAstrometry.SolverVizTools.ViewModels;
using WatneyAstrometry.SolverVizTools.Views;

namespace WatneyAstrometry.SolverVizTools.Services
{
    public class ViewProvider : IViewProvider
    {
        private Dictionary<Type, Type> _typeMap = new Dictionary<Type, Type>();

        public void Register<TView, TViewModel>() where TView : IControl where TViewModel : ViewModelBase
        {
            var vmType = typeof(TViewModel);
            var vType = typeof(TView);
            _typeMap.Add(vmType, vType);
        }

        public IWindow Instantiate<TViewModel>(TViewModel usingViewModel = null) where TViewModel : ViewModelBase
        {
            var vmType = typeof(TViewModel);
            var viewType = _typeMap[vmType];
            // var viewInstance = Locator.Current.GetService(viewType) as IControl;
            var viewInstance = (Window)Activator.CreateInstance(viewType)!;

            var wrapper = new WindowWrapper(viewInstance);

            if (usingViewModel == null)
            {
                var viewModelInstance = Locator.Current.GetService<TViewModel>();
                viewInstance.DataContext = viewModelInstance;
                viewModelInstance.OwnerWindow = wrapper;
            }
            else
            {
                viewInstance.DataContext = usingViewModel;
                usingViewModel.OwnerWindow = wrapper;
            }
            
            return wrapper;
        }
    }
}