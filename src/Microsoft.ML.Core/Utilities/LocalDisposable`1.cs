// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.ML.Internal.Utilities
{
    /// <summary>
    /// A helper structure to ensure disposable resources created by a method are disposed if the method throws.
    /// </summary>
    /// <remarks>
    /// <para>This object should only be used as a local in a <see langword="using"/> statement.</para>
    /// </remarks>
    /// <typeparam name="T">The resource type.</typeparam>
    internal struct LocalDisposable<T> : IDisposable
        where T : IDisposable
    {
        private bool _shouldDispose;

        public T Value;

        /// <summary>
        /// Initializes a new instance of <see cref="LocalDisposable{T}"/> with the specified resource.
        /// </summary>
        /// <param name="value">The disposable resource. This value may be <see langword="null"/> if <see cref="Value"/>
        /// will be initialized later, e.g. by calling a method that sets <see cref="Value"/> via an
        /// <see langword="out"/> parameter.</param>
        public LocalDisposable(T value)
        {
            _shouldDispose = true;
            Value = value;
        }

        /// <summary>
        /// Extracts the disposable value. Once the value is extracted, it will not be automatically disposed when the
        /// <see cref="LocalDisposable{T}"/> wrapper is disposed.
        /// </summary>
        /// <returns>Returns <see cref="Value"/>.</returns>
        public T Extract()
        {
            _shouldDispose = false;
            return Value;
        }

        void IDisposable.Dispose()
        {
            if (_shouldDispose)
                Value?.Dispose();
        }
    }
}
