// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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


using System.Windows;

namespace Papercut.AppLayer.Behaviors;

public class InteractivityBlurWindowOnDeactivate : InteractivityBlurBase<Window>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        this.AssociatedObject.Deactivated += this.AssociatedObjectOnDeactivated;
        this.AssociatedObject.Activated += this.AssociatedObjectOnActivated;
    }

    void AssociatedObjectOnActivated(object sender, EventArgs eventArgs)
    {
        this.ToggleBlurEffect(false);
    }

    void AssociatedObjectOnDeactivated(object sender, EventArgs eventArgs)
    {
        this.ToggleBlurEffect(true);
    }

    protected void ToggleBlurEffect(bool enable)
    {
            

        this.AssociatedObject.Effect = enable ? this.GetBlurEffect() : null;
    }
}