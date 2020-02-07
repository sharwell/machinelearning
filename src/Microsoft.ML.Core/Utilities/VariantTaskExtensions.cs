// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.ML
{
    public static class VariantTaskExtensions
    {
        public static ITask<TResult> AsVariantTask<TResult>(this Task<TResult> task)
            => new VariantTask<TResult>(task);

        private sealed class VariantTask<TResult> : ITask<TResult>, IAwaiter<TResult>, ICriticalNotifyCompletion
        {
            private readonly Task<TResult> _task;
            private readonly TaskAwaiter<TResult> _awaiter;

            public VariantTask(Task<TResult> task)
            {
                _task = task;
                _awaiter = task.GetAwaiter();
            }

            public bool IsCompleted => _awaiter.IsCompleted;

            public IAwaiter<TResult> GetAwaiter()
                => this;

            public TResult GetResult()
                => _awaiter.GetResult();

            public void OnCompleted(Action continuation)
                => _awaiter.OnCompleted(continuation);

            public void UnsafeOnCompleted(Action continuation)
                => _awaiter.UnsafeOnCompleted(continuation);
        }
    }
}
