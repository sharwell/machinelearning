﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.ML.Core.Data;
using Microsoft.ML.Runtime;
using Microsoft.ML.StaticPipe.Runtime;
using Microsoft.ML.Transforms.Categorical;
using Microsoft.ML.Transforms.Conversions;
using Microsoft.ML.Transforms.Projections;
using System;
using System.Collections.Generic;

namespace Microsoft.ML.StaticPipe
{
    /// <summary>
    /// Extensions for statically typed <see cref="LpNormalizingEstimator"/>.
    /// </summary>
    public static class LpNormalizerExtensions
    {
        private sealed class OutPipelineColumn : Vector<float>
        {
            public readonly Vector<float> Input;

            public OutPipelineColumn(Vector<float> input, LpNormalizingEstimatorBase.NormalizerKind normKind, bool subMean)
                : base(new Reconciler(normKind, subMean), input)
            {
                Input = input;
            }
        }

        private sealed class Reconciler : EstimatorReconciler
        {
            private readonly LpNormalizingEstimatorBase.NormalizerKind _normKind;
            private readonly bool _subMean;

            public Reconciler(LpNormalizingEstimatorBase.NormalizerKind normKind, bool subMean)
            {
                _normKind = normKind;
                _subMean = subMean;
            }

            public override IEstimator<ITransformer> Reconcile(IHostEnvironment env,
                PipelineColumn[] toOutput,
                IReadOnlyDictionary<PipelineColumn, string> inputNames,
                IReadOnlyDictionary<PipelineColumn, string> outputNames,
                IReadOnlyCollection<string> usedNames)
            {
                Contracts.Assert(toOutput.Length == 1);

                var pairs = new List<(string input, string output)>();
                foreach (var outCol in toOutput)
                    pairs.Add((inputNames[((OutPipelineColumn)outCol).Input], outputNames[outCol]));

                return new LpNormalizingEstimator(env, pairs.ToArray(), _normKind, _subMean);
            }
        }

        /// <include file='doc.xml' path='doc/members/member[@name="LpNormalize"]/*'/>
        /// <param name="input">The column to apply to.</param>
        /// <param name="normKind">Type of norm to use to normalize each sample.</param>
        /// <param name="subMean">Subtract mean from each value before normalizing.</param>
        public static Vector<float> LpNormalize(this Vector<float> input,
            LpNormalizingEstimatorBase.NormalizerKind normKind = LpNormalizingEstimatorBase.Defaults.NormKind,
            bool subMean = LpNormalizingEstimatorBase.Defaults.LpSubstractMean) => new OutPipelineColumn(input, normKind, subMean);
    }

    /// <summary>
    /// Extensions for statically typed <see cref="GlobalContrastNormalizingEstimator"/>.
    /// </summary>
    public static class GlobalContrastNormalizerExtensions
    {
        private sealed class OutPipelineColumn : Vector<float>
        {
            public readonly Vector<float> Input;

            public OutPipelineColumn(Vector<float> input, bool subMean, bool useStdDev, float scale)
                : base(new Reconciler(subMean, useStdDev, scale), input)
            {
                Input = input;
            }
        }

        private sealed class Reconciler : EstimatorReconciler
        {
            private readonly bool _subMean;
            private readonly bool _useStdDev;
            private readonly float _scale;

            public Reconciler(bool subMean, bool useStdDev, float scale)
            {
                _subMean = subMean;
                _useStdDev = useStdDev;
                _scale = scale;
            }

            public override IEstimator<ITransformer> Reconcile(IHostEnvironment env,
                PipelineColumn[] toOutput,
                IReadOnlyDictionary<PipelineColumn, string> inputNames,
                IReadOnlyDictionary<PipelineColumn, string> outputNames,
                IReadOnlyCollection<string> usedNames)
            {
                Contracts.Assert(toOutput.Length == 1);

                var pairs = new List<(string input, string output)>();
                foreach (var outCol in toOutput)
                    pairs.Add((inputNames[((OutPipelineColumn)outCol).Input], outputNames[outCol]));

                return new GlobalContrastNormalizingEstimator(env, pairs.ToArray(), _subMean, _useStdDev, _scale);
            }
        }

        /// <include file='doc.xml' path='doc/members/member[@name="GcNormalize"]/*'/>
        /// <param name="input">The column to apply to.</param>
        /// <param name="subMean">Subtract mean from each value before normalizing.</param>
        /// <param name="useStdDev">Normalize by standard deviation rather than L2 norm.</param>
        /// <param name="scale">Scale features by this value.</param>
        public static Vector<float> GlobalContrastNormalize(this Vector<float> input,
            bool subMean = LpNormalizingEstimatorBase.Defaults.GcnSubstractMean,
            bool useStdDev = LpNormalizingEstimatorBase.Defaults.UseStdDev,
            float scale = LpNormalizingEstimatorBase.Defaults.Scale) => new OutPipelineColumn(input, subMean, useStdDev, scale);
    }

    public static class CategoricalStaticExtensions
    {
        public enum OneHotVectorOutputKind : byte
        {
            /// <summary>
            /// Output is a bag (multi-set) vector
            /// </summary>
            Bag = 1,

            /// <summary>
            /// Output is an indicator vector
            /// </summary>
            Ind = 2,

            /// <summary>
            /// Output is binary encoded
            /// </summary>
            Bin = 4,
        }

        public enum OneHotScalarOutputKind : byte
        {
            /// <summary>
            /// Output is an indicator vector
            /// </summary>
            Ind = 2,

            /// <summary>
            /// Output is binary encoded
            /// </summary>
            Bin = 4,
        }

        private const KeyValueOrder DefSort = (KeyValueOrder)ValueToKeyMappingEstimator.Defaults.Sort;
        private const int DefMax = ValueToKeyMappingEstimator.Defaults.MaxNumTerms;
        private const OneHotVectorOutputKind DefOut = (OneHotVectorOutputKind)OneHotEncodingEstimator.Defaults.OutKind;

        private readonly struct Config
        {
            public readonly KeyValueOrder Order;
            public readonly int Max;
            public readonly OneHotVectorOutputKind OutputKind;
            public readonly Action<ValueToKeyMappingTransformer.TermMap> OnFit;

            public Config(OneHotVectorOutputKind outputKind, KeyValueOrder order, int max, Action<ValueToKeyMappingTransformer.TermMap> onFit)
            {
                OutputKind = outputKind;
                Order = order;
                Max = max;
                OnFit = onFit;
            }
        }

        private static Action<ValueToKeyMappingTransformer.TermMap> Wrap<T>(ToKeyFitResult<T>.OnFit onFit)
        {
            if (onFit == null)
                return null;
            // The type T asociated with the delegate will be the actual value type once #863 goes in.
            // However, until such time as #863 goes in, it would be too awkward to attempt to extract the metadata.
            // For now construct the useless object then pass it into the delegate.
            return map => onFit(new ToKeyFitResult<T>(map));
        }

        private interface ICategoricalCol
        {
            PipelineColumn Input { get; }
            Config Config { get; }
        }

        private sealed class ImplScalar<T> : Vector<float>, ICategoricalCol
        {
            public PipelineColumn Input { get; }
            public Config Config { get; }
            public ImplScalar(PipelineColumn input, Config config) : base(Rec.Inst, input)
            {
                Input = input;
                Config = config;
            }
        }

        private sealed class ImplVector<T> : Vector<float>, ICategoricalCol
        {
            public PipelineColumn Input { get; }
            public Config Config { get; }
            public ImplVector(PipelineColumn input, Config config) : base(Rec.Inst, input)
            {
                Input = input;
                Config = config;
            }
        }

        private sealed class Rec : EstimatorReconciler
        {
            public static readonly Rec Inst = new Rec();

            public override IEstimator<ITransformer> Reconcile(IHostEnvironment env, PipelineColumn[] toOutput,
                IReadOnlyDictionary<PipelineColumn, string> inputNames, IReadOnlyDictionary<PipelineColumn, string> outputNames, IReadOnlyCollection<string> usedNames)
            {
                var infos = new OneHotEncodingEstimator.ColumnInfo[toOutput.Length];
                Action<ValueToKeyMappingTransformer> onFit = null;
                for (int i = 0; i < toOutput.Length; ++i)
                {
                    var tcol = (ICategoricalCol)toOutput[i];
                    infos[i] = new OneHotEncodingEstimator.ColumnInfo(inputNames[tcol.Input], outputNames[toOutput[i]], (OneHotEncodingTransformer.OutputKind)tcol.Config.OutputKind,
                        tcol.Config.Max, (ValueToKeyMappingTransformer.SortOrder)tcol.Config.Order);
                    if (tcol.Config.OnFit != null)
                    {
                        int ii = i; // Necessary because if we capture i that will change to toOutput.Length on call.
                        onFit += tt => tcol.Config.OnFit(tt.GetTermMap(ii));
                    }
                }
                var est = new OneHotEncodingEstimator(env, infos);
                if (onFit != null)
                    est.WrapTermWithDelegate(onFit);
                return est;
            }
        }

        /// <summary>
        /// Converts the categorical value into an indicator array by building a dictionary of categories based on the data and using the id in the dictionary as the index in the array.
        /// </summary>
        /// <param name="input">Incoming data.</param>
        /// <param name="outputKind">Specify the output type of indicator array: array or binary encoded data.</param>
        /// <param name="order">How the Id for each value would be assigined: by occurrence or by value.</param>
        /// <param name="maxItems">Maximum number of ids to keep during data scanning.</param>
        /// <param name="onFit">Called upon fitting with the learnt enumeration on the dataset.</param>
        public static Vector<float> OneHotEncoding(this Scalar<string> input, OneHotScalarOutputKind outputKind = (OneHotScalarOutputKind)DefOut, KeyValueOrder order = DefSort,
            int maxItems = DefMax, ToKeyFitResult<ReadOnlyMemory<char>>.OnFit onFit = null)
        {
            Contracts.CheckValue(input, nameof(input));
            return new ImplScalar<string>(input, new Config((OneHotVectorOutputKind)outputKind, order, maxItems, Wrap(onFit)));
        }

        /// <summary>
        /// Converts the categorical value into an indicator array by building a dictionary of categories based on the data and using the id in the dictionary as the index in the array.
        /// </summary>
        /// <param name="input">Incoming data.</param>
        /// <param name="outputKind">Specify the output type of indicator array: Multiarray, array or binary encoded data.</param>
        /// <param name="order">How the Id for each value would be assigined: by occurrence or by value.</param>
        /// <param name="maxItems">Maximum number of ids to keep during data scanning.</param>
        /// <param name="onFit">Called upon fitting with the learnt enumeration on the dataset.</param>
        public static Vector<float> OneHotEncoding(this Vector<string> input, OneHotVectorOutputKind outputKind = DefOut, KeyValueOrder order = DefSort, int maxItems = DefMax,
            ToKeyFitResult<ReadOnlyMemory<char>>.OnFit onFit = null)
        {
            Contracts.CheckValue(input, nameof(input));
            return new ImplVector<string>(input, new Config(outputKind, order, maxItems, Wrap(onFit)));
        }
    }
}
