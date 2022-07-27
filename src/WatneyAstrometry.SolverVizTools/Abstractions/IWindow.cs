using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace WatneyAstrometry.SolverVizTools.Abstractions
{
    public interface IWindow
    {
        public object NativeWindow { get; }

        void Close();
        void Close(object dialogResult);

        WindowStartupLocation WindowStartupLocation { get; set; }

        Task<TResult> ShowDialog<TResult>(IWindow owner);

        //TViewModel ViewModel { get; }
        //bool WasClosed { get; }
        //bool? DialogResult { get; set; }

        //event EventHandler OnWindowLoaded;
        //event EventHandler<CancelEventArgs> OnWindowClosing;
        //event EventHandler OnWindowClosed;

        //void Show();
        //void Close();
        //bool ShowModal();

        //XY? GetControlSize(string controlName);
    }
}
