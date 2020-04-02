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

namespace Papercut.Helpers
{
    using System;
    using System.Windows;

    using Autofac;

    using Caliburn.Micro;

    using MahApps.Metro.Controls;

    public class ViewModelWindowManager : WindowManager, IViewModelWindowManager
    {
        readonly ILifetimeScope _lifetimeScope;

        ResourceDictionary[] _resourceDictionaries;

        public ViewModelWindowManager(
            ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope;
        }

        public bool? ShowDialogWithViewModel<TViewModel>(
            Action<TViewModel> setViewModel = null,
            object context = null)
            where TViewModel : PropertyChangedBase
        {
            var viewModel = _lifetimeScope.Resolve<TViewModel>();
            setViewModel?.Invoke(viewModel);
            return ShowDialog(viewModel, context);
        }

        public void ShowWindowWithViewModel<TViewModel>(
            Action<TViewModel> setViewModel = null,
            object context = null)
            where TViewModel : PropertyChangedBase
        {
            var viewModel = _lifetimeScope.Resolve<TViewModel>();
            setViewModel?.Invoke(viewModel);
            ShowWindow(viewModel, context);
        }

        public void ShowPopupWithViewModel<TViewModel>(
            Action<TViewModel> setViewModel = null,
            object context = null)
            where TViewModel : PropertyChangedBase
        {
            var viewModel = _lifetimeScope.Resolve<TViewModel>();
            setViewModel?.Invoke(viewModel);
            ShowPopup(viewModel, context);
        }
    }
}