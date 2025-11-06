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

using System.Windows.Media.Animation;

namespace Papercut.Helpers;

/// <summary>
/// A control that displays a zoom percentage indicator that fades in and out
/// </summary>
public partial class ZoomIndicator : UserControl
{
    private DispatcherTimer? _fadeTimer;
    private Storyboard? _fadeInStoryboard;
    private Storyboard? _fadeOutStoryboard;

    public ZoomIndicator()
    {
        InitializeComponent();
        InitializeAnimations();
    }

    private void InitializeAnimations()
    {
        // Create fade-in animation
        _fadeInStoryboard = new Storyboard();
        var fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(150),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(fadeIn, IndicatorBorder);
        Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
        _fadeInStoryboard.Children.Add(fadeIn);

        // Create fade-out animation
        _fadeOutStoryboard = new Storyboard();
        var fadeOut = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(fadeOut, IndicatorBorder);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
        _fadeOutStoryboard.Children.Add(fadeOut);

        // Create timer for auto-fade
        _fadeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(800)
        };
        _fadeTimer.Tick += (s, e) =>
        {
            _fadeTimer?.Stop();
            _fadeOutStoryboard?.Begin();
        };
    }

    /// <summary>
    /// Shows the zoom indicator with the specified zoom percentage
    /// </summary>
    /// <param name="zoomPercentage">Zoom level as a percentage (e.g., 100 for 100%)</param>
    public void ShowZoom(int zoomPercentage)
    {
        // Update text
        ZoomText.Text = $"{zoomPercentage}%";

        // Stop any running animations and timer
        _fadeTimer?.Stop();
        _fadeOutStoryboard?.Stop();

        // If already visible, just reset the timer without re-animating
        if (IndicatorBorder.Opacity > 0.9)
        {
            // Already visible, just restart the fade-out timer
            _fadeTimer?.Start();
        }
        else
        {
            // Not visible, fade in
            _fadeInStoryboard?.Begin();
            _fadeTimer?.Start();
        }
    }

    /// <summary>
    /// Shows the zoom indicator with the specified zoom factor (e.g., 1.0 = 100%)
    /// </summary>
    /// <param name="zoomFactor">Zoom factor where 1.0 = 100%</param>
    public void ShowZoomFactor(double zoomFactor)
    {
        ShowZoom((int)Math.Round(zoomFactor * 100));
    }

    /// <summary>
    /// Shows the zoom indicator based on font size relative to default size
    /// </summary>
    /// <param name="fontSize">Current font size</param>
    /// <param name="defaultSize">Default/base font size</param>
    public void ShowZoomFromFontSize(double fontSize, double defaultSize)
    {
        // Guard against division by zero or near-zero default size
        if (defaultSize <= double.Epsilon)
        {
            Log.Warning("ShowZoomFromFontSize called with invalid defaultSize: {DefaultSize}. Using 100% as fallback.", defaultSize);
            ShowZoom(100);
            return;
        }

        var percentage = (int)Math.Round((fontSize / defaultSize) * 100);
        ShowZoom(percentage);
    }
}
