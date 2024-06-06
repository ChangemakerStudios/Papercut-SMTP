﻿// Papercut
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


using Caliburn.Micro;

namespace Papercut.Helpers
{
    public interface IViewModelWindowManager : IWindowManager
    {
        Task<bool?> ShowDialogWithViewModel<TViewModel>(
            Action<TViewModel>? setViewModel = null,
            object? context = null)
            where TViewModel : PropertyChangedBase;

        Task ShowWindowWithViewModelAsync<TViewModel>(
            Action<TViewModel>? setViewModel = null,
            object? context = null)
            where TViewModel : PropertyChangedBase;

        Task ShowPopupWithViewModel<TViewModel>(
            Action<TViewModel>? setViewModel = null,
            object? context = null)
            where TViewModel : PropertyChangedBase;
    }
}