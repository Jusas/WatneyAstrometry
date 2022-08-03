using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Splat;
using WatneyAstrometry.SolverVizTools.ViewModels;

namespace WatneyAstrometry.SolverVizTools.Views
{
    public partial class SolveProcessView : UserControl
    {
        private bool _logExpanded = false;
        public double ZoomLevel { get; set; } = 1;

        private SolveProcessViewModel _viewModel => DataContext as SolveProcessViewModel;

        public SolveProcessView()
        {
            InitializeComponent();
            SetupDragAndDrop();
            
        }

        private void SetImageControlSize()
        {
            var solverImage = this.GetControl<Image>("SolverImage");
            var imageScrollView = this.GetControl<ScrollViewer>("ImageScrollView");
            solverImage.Width = imageScrollView.Bounds.Width * ZoomLevel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void SetupDragAndDrop()
        {
            AddHandler(DragDrop.DragOverEvent, OnDragOver);
            AddHandler(DragDrop.DropEvent, OnDragDrop);
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            var dragDropZone = "ImageSelectionArea";
            if (e.Source is Control c && (c.Name == dragDropZone || ParentNameIs(c.Parent, dragDropZone)))
            {
                e.DragEffects &= DragDropEffects.Move;

                if (!e.Data.Contains(DataFormats.FileNames))
                {
                    e.DragEffects = DragDropEffects.None;
                }

                var filenames = e.Data.GetFileNames();
                var file = filenames.First();

                //var viewModel = Locator.Current.GetService<SolveProcessViewModel>();
                _viewModel.OpenImage(file);
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            var dragDropZone = "ImageSelectionArea";
            if (e.Source is Control c && (c.Name == dragDropZone || ParentNameIs(c.Parent, dragDropZone)))
            {
                e.DragEffects &= DragDropEffects.Move;

                if (!e.Data.Contains(DataFormats.FileNames))
                {
                    e.DragEffects = DragDropEffects.None;
                }
            }
        }

        private bool ParentNameIs(IControl control, string name)
        {
            if (control == null)
                return false;

            if (control.Name == name)
                return true;

            if (control.Parent != null)
                return ParentNameIs(control.Parent, name);

            return false;
        }

        private void OnExpandLogClicked(object sender, RoutedEventArgs e)
        {
            _logExpanded = !_logExpanded;
            var log = this.GetControl<ListBox>("Log");
            if (_logExpanded)
                log.Height *= 3;
            else
                log.Height /= 3;
        }


        private IPointer _imagePanTrackedPointer;
        private Point _imagePanStartingPos;
        private void Image_OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.Pointer.IsPrimary)
            {
                _imagePanTrackedPointer = e.Pointer;
                _imagePanStartingPos = e.GetPosition((IVisual)sender);
            }
        }

        private void Image_OnPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            _imagePanTrackedPointer = null;
        }

        private void Image_OnPointerMoved(object sender, PointerEventArgs e)
        {
            
            if (e.Pointer == _imagePanTrackedPointer)
            {
                var scrollViewer = this.GetControl<ScrollViewer>("ImageScrollView");
                var pos = e.GetPosition((IVisual)sender);
                var delta = _imagePanStartingPos - pos;
                e.Handled = true;
                scrollViewer.Offset += new Vector(delta.X, delta.Y);
            }
        }

        private void Image_OnPointerCaptureLost(object sender, PointerCaptureLostEventArgs e)
        {
            _imagePanTrackedPointer = null;
        }

        private void ZoomIn_OnClick(object sender, RoutedEventArgs e)
        {
            if(ZoomLevel < 4)
                ZoomLevel *= 2;

            SetImageControlSize();
        }

        private void ZoomOut_OnClick(object sender, RoutedEventArgs e)
        {
            if (ZoomLevel > 0.5)
                ZoomLevel /= 2;

            SetImageControlSize();
        }

        private void ImageScrollView_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentDelta != Vector.Zero)
            {
                var scrollViewer = (ScrollViewer)sender;
                var newExtents = scrollViewer.Extent;
                var oldExtents = new Vector(scrollViewer.Extent.Width - e.ExtentDelta.X, scrollViewer.Extent.Height - e.ExtentDelta.Y);

                if (oldExtents == Vector.Zero)
                    return;

                if (e.ViewportDelta != Vector.Zero)
                    return;

                var sign = e.ExtentDelta.X < 0 ? 0 : 1;

                var scrollPos = scrollViewer.Offset;
                var relativeScrollX = scrollPos.X / oldExtents.X;
                var relativeScrollY = scrollPos.Y / oldExtents.Y;

                var newScrollPos = new Vector(relativeScrollX * newExtents.Width + (scrollViewer.Viewport.Width * 0.5 * sign), relativeScrollY * newExtents.Height + (scrollViewer.Viewport.Height * 0.5 * sign));
                scrollViewer.Offset = newScrollPos;
            }
        }

        private void SolverImage_OnEffectiveViewportChanged(object sender, EffectiveViewportChangedEventArgs e)
        {
            SetImageControlSize();
        }
    }
}
