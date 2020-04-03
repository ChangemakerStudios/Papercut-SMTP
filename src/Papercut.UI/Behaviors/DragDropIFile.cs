// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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

namespace Papercut.Behaviors
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Interactivity;

    using Papercut.Core.Domain.Message;
    using Papercut.Helpers;

    public class DragDropIFile : Behavior<ListBox>
    {
        Point? _dragStartPoint;

        protected override void OnAttached()
        {
            base.OnAttached();

            // bind it!
            AssociatedObject.PreviewMouseMove += ListBoxPreviewMouseMove;
            AssociatedObject.PreviewMouseLeftButtonDown += ListBoxPreviewLeftMouseDown;
            AssociatedObject.PreviewMouseUp += ListBoxPreviewMouseUp;
        }

        void ListBoxPreviewLeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            var parent = sender as ListBox;

            if (parent == null) return;

            if (_dragStartPoint == null) _dragStartPoint = e.GetPosition(parent);
        }

        void ListBoxPreviewMouseMove(object sender, MouseEventArgs e)
        {
            var parent = sender as ListBox;
            if (parent == null || _dragStartPoint == null) return;

            if (((DependencyObject)e.OriginalSource).FindAncestor<ScrollBar>() != null) return;

            Point dragPoint = e.GetPosition(parent);

            Vector potentialDragLength = dragPoint - _dragStartPoint.Value;

            if (potentialDragLength.Length > 10)
            {
                // Get the object source for the selected item
                var entry = parent.GetObjectDataFromPoint(_dragStartPoint.Value) as IFile;

                // If the data is not null then start the drag drop operation
                if (entry != null && !string.IsNullOrWhiteSpace(entry.File))
                {
                    var dataObject = new DataObject(DataFormats.FileDrop, new[] { entry.File });
                    DragDrop.DoDragDrop(parent, dataObject, DragDropEffects.Copy);
                }

                _dragStartPoint = null;
            }
        }

        void ListBoxPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = null;
        }
    }
}