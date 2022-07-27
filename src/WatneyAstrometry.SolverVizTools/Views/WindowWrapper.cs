using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using WatneyAstrometry.SolverVizTools.Abstractions;

namespace WatneyAstrometry.SolverVizTools.Views
{
    public class WindowWrapper : IWindow
    {
        private readonly Window _window;
        
        public object NativeWindow => _window;

        public WindowStartupLocation WindowStartupLocation
        {
            get => _window.WindowStartupLocation;
            set => _window.WindowStartupLocation = value;
        }

        public Task<TResult> ShowDialog<TResult>(IWindow owner)
        {
            return _window.ShowDialog<TResult>(((owner as WindowWrapper)!)._window);
        }

        public WindowWrapper(Window window)
        {
            _window = window;
        }


        public void Close()
        {
            _window.Close();
        }

        public void Close(object dialogResult)
        {
            _window.Close(dialogResult);
        }
    }
}
