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


using Papercut.Common.Extensions;

namespace Papercut.Common.Domain;

public class ExecutionResult<T> : ExecutionResult
{
    public ExecutionResult(bool isSuccess, T? value, IEnumerable<string>? errors = null)
        : base(isSuccess, errors)
    {
        if (isSuccess)
        {
            ArgumentNullException.ThrowIfNull(value, nameof(value));
            this.Value = value!;
        }
    }

    public T Value { get; }
}

public class ExecutionResult
{
    protected ExecutionResult(bool isSuccess, IEnumerable<string>? errors)
    {
        this.IsSuccess = isSuccess;

        if (!this.IsSuccess)
        {
            this.Errors = errors.IfNullEmpty().ToArray();
        }
    }

    public IReadOnlyCollection<string> Errors { get; }

    public bool IsSuccess { get; }

    public bool IsFailed => !IsSuccess;

    public static ExecutionResult Failure(params string[] errors)
    {
        return new ExecutionResult(false, errors);
    }

    public static ExecutionResult Success()
    {
        return new ExecutionResult(true, null);
    }

    public static ExecutionResult<T> Failure<T>(params string[] errors)
    {
        return new ExecutionResult<T>(false, default, errors);
    }

    public static ExecutionResult<T> Success<T>(T result)
    {
        return new ExecutionResult<T>(true, result);
    }
}