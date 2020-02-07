// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.ML.Runtime;

namespace Microsoft.ML.Data
{
    /// <summary>
    /// The trivial implementation of <see cref="IEstimator{TTransformer}"/> that already has
    /// the transformer and returns it on every call to <see cref="FitAsync(IDataView)"/>.
    ///
    /// Concrete implementations still have to provide the schema propagation mechanism, since
    /// there is no easy way to infer it from the transformer.
    /// </summary>
    public abstract class TrivialEstimator<TTransformer> : IEstimator<TTransformer>
        where TTransformer : class, ITransformer
    {
        [BestFriend]
        private protected readonly IHost Host;
        [BestFriend]
        private protected readonly TTransformer Transformer;

        [BestFriend]
        private protected TrivialEstimator(IHost host, TTransformer transformer)
        {
            Contracts.AssertValue(host);

            Host = host;
            Host.CheckValue(transformer, nameof(transformer));
            Transformer = transformer;
        }

        public async ITask<TTransformer> FitAsync(IDataView input)
        {
            Host.CheckValue(input, nameof(input));
            // Validate input schema.
            Transformer.GetOutputSchema(input.Schema);
            return Transformer;
        }

        public abstract Task<SchemaShape> GetOutputSchemaAsync(SchemaShape inputSchema);
    }
}
