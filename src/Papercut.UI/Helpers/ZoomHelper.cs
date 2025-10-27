// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Windows.Input;

namespace Papercut.Helpers;

/// <summary>
/// Helper class to manage zoom functionality for message detail views
/// </summary>
public static class ZoomHelper
{
    /// <summary>
    /// Calculates the new zoom level based on mouse wheel delta
    /// </summary>
    /// <param name="currentZoom">Current zoom level</param>
    /// <param name="delta">Mouse wheel delta (positive = zoom in, negative = zoom out)</param>
    /// <param name="increment">Amount to increment per scroll step</param>
    /// <param name="minZoom">Minimum zoom level</param>
    /// <param name="maxZoom">Maximum zoom level</param>
    /// <returns>New zoom level clamped to min/max bounds</returns>
    public static double CalculateNewZoom(double currentZoom, int delta, double increment, double minZoom, double maxZoom)
    {
        double zoomDelta = delta > 0 ? increment : -increment;
        return Math.Max(minZoom, Math.Min(maxZoom, currentZoom + zoomDelta));
    }

    /// <summary>
    /// Checks if the Ctrl key modifier is pressed (for zoom activation)
    /// </summary>
    public static bool IsZoomModifierPressed()
    {
        return (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
    }

    /// <summary>
    /// Constants for WebView2 zoom (factor-based)
    /// </summary>
    public static class WebView2Zoom
    {
        public const double DefaultZoom = 1.0;
        public const double MinZoom = 0.25;
        public const double MaxZoom = 5.0;
        public const double Increment = 0.05;
    }

    /// <summary>
    /// Constants for AvalonEdit zoom (font size-based, in DIPs)
    /// Note: Using even DIP values for consistency (14 DIPs ≈ 10.5pt)
    /// </summary>
    public static class AvalonEditZoom
    {
        public const double DefaultFontSize = 14.0;
        public const double MinFontSize = 8.0;
        public const double MaxFontSize = 56.0;
        public const double Increment = 1.0;
    }
}
