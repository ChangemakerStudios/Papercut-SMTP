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


using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Microsoft.Xaml.Behaviors;

using Papercut.Core.Domain.Message;

namespace Papercut.AppLayer.Behaviors;

public class DragDropIFile : Behavior<ListBox>
{
    Point? _dragStartPoint;

    protected override void OnAttached()
    {
        base.OnAttached();

        // bind it!
        this.AssociatedObject.PreviewMouseMove += this.ListBoxPreviewMouseMove;
        this.AssociatedObject.PreviewMouseLeftButtonDown += this.ListBoxPreviewLeftMouseDown;
        this.AssociatedObject.PreviewMouseUp += this.ListBoxPreviewMouseUp;
    }

    void ListBoxPreviewLeftMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBox parent) return;

        if (this._dragStartPoint == null) this._dragStartPoint = e.GetPosition(parent);
    }

    void ListBoxPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not ListBox parent || this._dragStartPoint == null) return;

        if (((DependencyObject)e.OriginalSource).FindAncestor<ScrollBar>() != null) return;

        Point dragPoint = e.GetPosition(parent);

        Vector potentialDragLength = dragPoint - this._dragStartPoint.Value;

        if (potentialDragLength.Length > 10)
        {
            // Get the object source for the selected item

            // If the data is not null then start the drag drop operation
            if (parent.GetObjectDataFromPoint(this._dragStartPoint.Value) is IFile entry && !string.IsNullOrWhiteSpace(entry.File))
            {
                var dataObject = new DataObject(DataFormats.FileDrop, new[] { entry.File });
                DragDrop.DoDragDrop(parent, dataObject, DragDropEffects.Copy);
            }

            this._dragStartPoint = null;
        }
    }

    void ListBoxPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        this._dragStartPoint = null;
    }
}