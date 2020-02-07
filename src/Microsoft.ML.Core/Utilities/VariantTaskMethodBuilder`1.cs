// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ML
{
    public struct VariantTaskMethodBuilder<TResult>
    {
        private AsyncTaskMethodBuilder<TResult> _taskMethodBuilder;
        private ITask<TResult> _task;

        private VariantTaskMethodBuilder(AsyncTaskMethodBuilder<TResult> taskMethodBuilder)
        {
            _taskMethodBuilder = taskMethodBuilder;
            _task = null;
        }

        public static VariantTaskMethodBuilder<TResult> Create()
            => new VariantTaskMethodBuilder<TResult>(AsyncTaskMethodBuilder<TResult>.Create());

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
            => _taskMethodBuilder.Start(ref stateMachine);

        public void SetStateMachine(IAsyncStateMachine stateMachine)
            => _taskMethodBuilder.SetStateMachine(stateMachine);

        public void SetResult(TResult result)
            => _taskMethodBuilder.SetResult(result);

        public void SetException(Exception exception)
            => _taskMethodBuilder.SetException(exception);

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => _taskMethodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => _taskMethodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

        public ITask<TResult> Task
        {
            get
            {
                if (_task is null)
                    Interlocked.CompareExchange(ref _task, _taskMethodBuilder.Task.AsVariantTask(), null);

                return _task;
            }
        }
    }
}
