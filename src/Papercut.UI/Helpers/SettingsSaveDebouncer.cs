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


using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Papercut.Helpers;

/// <summary>
/// Provides debounced saving of settings to reduce I/O during rapid changes
/// </summary>
/// <typeparam name="T">Type of value being debounced (e.g., double for zoom)</typeparam>
public class SettingsSaveDebouncer<T> : IDisposable
{
    private readonly Subject<T> _changeSubject = new();
    private readonly IDisposable _subscription;

    /// <summary>
    /// Creates a new debouncer that throttles settings saves
    /// </summary>
    /// <param name="saveAction">Action to execute after debounce period (e.g., save to Settings)</param>
    /// <param name="debounceMilliseconds">Debounce delay in milliseconds (default: 500ms)</param>
    public SettingsSaveDebouncer(Action<T> saveAction, int debounceMilliseconds = 500)
    {
        _subscription = _changeSubject
            .Throttle(TimeSpan.FromMilliseconds(debounceMilliseconds))
            .ObserveOn(System.Reactive.Concurrency.Scheduler.CurrentThread)
            .Subscribe(saveAction);
    }

    /// <summary>
    /// Notifies the debouncer of a new value change
    /// </summary>
    /// <param name="value">The new value</param>
    public void OnValueChanged(T value)
    {
        _changeSubject.OnNext(value);
    }

    /// <summary>
    /// Disposes resources and completes the observable stream
    /// </summary>
    public void Dispose()
    {
        _changeSubject?.OnCompleted();
        _subscription?.Dispose();
    }
}
