// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ML.Internal.Utilities
{
    [BestFriend]
    internal static class SemaphoreSlimExtensions
    {
        public static Releaser DisposableWait(this SemaphoreSlim semaphore, CancellationToken cancellationToken)
        {
            semaphore.Wait(cancellationToken);
            return new Releaser(semaphore);
        }

        public static async ValueTask<Releaser> DisposableWaitAsync(this SemaphoreSlim semaphore, CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            return new Releaser(semaphore);
        }

        public readonly struct Releaser : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            public Releaser(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
                => _semaphore.Release();
        }
    }
}
