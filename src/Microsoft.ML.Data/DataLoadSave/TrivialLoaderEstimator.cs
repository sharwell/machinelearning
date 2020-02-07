// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.ML.Data
{
    /// <summary>
    /// The trivial wrapper for a <see cref="IDataLoader{TSource}"/> that acts as an estimator and ignores the source.
    /// </summary>
    internal sealed class TrivialLoaderEstimator<TSource, TLoader> : IDataLoaderEstimator<TSource, TLoader>
        where TLoader : IDataLoader<TSource>
    {
        public TLoader Loader { get; }

        public TrivialLoaderEstimator(TLoader loader)
        {
            Loader = loader;
        }

        public async ITask<TLoader> FitAsync(TSource input) => Loader;

        public async Task<SchemaShape> GetOutputSchemaAsync() => SchemaShape.Create(Loader.GetOutputSchema());
    }
}
