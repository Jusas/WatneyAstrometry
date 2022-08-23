using System;
using Avalonia;
using Avalonia.Controls;

namespace WatneyAstrometry.SolverVizTools.Utils;

public static class WindowWorkarounds
{
    /// <summary>
    /// From: https://github.com/AvaloniaUI/Avalonia/issues/6433#issuecomment-1001766862
    /// Essentially the window centering bugs out in Linux. This fixes it.
    /// </summary>
    /// <param name="window"></param>
    public static void ApplyWindowCenteringWorkaround(Window window)
    {
        if (OperatingSystem.IsWindows())
        {
            // Not needed for Windows
            return;
        }

        var scale = window.PlatformImpl?.DesktopScaling ?? 1.0;
        var pOwner = window.Owner?.PlatformImpl;
        if (pOwner != null)
        {
            scale = pOwner.DesktopScaling;
        }
        var rect = new PixelRect(PixelPoint.Origin,
            PixelSize.FromSize(window.ClientSize, scale));
        if (window.WindowStartupLocation == WindowStartupLocation.CenterScreen)
        {
            var screen = window.Screens.ScreenFromPoint(pOwner?.Position ?? window.Position);
            if (screen == null)
            {
                return;
            }
            window.Position = screen.WorkingArea.CenterRect(rect).Position;
        }
        else
        {
            if (pOwner == null ||
                window.WindowStartupLocation != WindowStartupLocation.CenterOwner)
            {
                return;
            }
            window.Position = new PixelRect(pOwner.Position,
                PixelSize.FromSize(pOwner.ClientSize, scale)).CenterRect(rect).Position;
        }
    }
}