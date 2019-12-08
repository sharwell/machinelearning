// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.ML.Runtime;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.ML.TextAnalytics
{
#pragma warning disable MSML_ParameterLocalVarName // Parameter or local variable name not standard
#pragma warning disable MSML_PrivateFieldName // Private field name not in: _camelCase format
#pragma warning disable MSML_GeneralName // This name should be PascalCased
    internal static class ManagedLdaInterface
    {
        private static class lda
        {
            private static readonly double[] cof =
            {
                76.18009172947146,
                -86.50532032941677,
                24.01409824083091,
                -1.231739572450155,
                0.1208650973866179e-2,
                -0.5395239384953e-5,
            };

            public static double LogGamma(double xx)
            {
                int j;
                double x;
                double y;
                double tmp1;
                double ser;
                y = xx;
                x = xx;
                tmp1 = x + 5.5;
                tmp1 -= (x + 0.5) * Math.Log(tmp1);
                ser = 1.000000000190015;
                for (j = 0; j < cof.Length; j++)
                    ser += cof[j] / ++y;

                return -tmp1 + Math.Log(2.5066282746310005 * ser / x);
            }

            public static double get_time()
            {
                throw new NotImplementedException();
            }
        }

        private sealed class LDAEngineAtomics
        {
            public double doc_ll_;
            public double word_ll_;

            // # of tokens processed in a Clock() call.
            public double num_tokens_clock_;
            public double thread_counter_;

            public readonly object global_mutex_ = new object();
        }

        private sealed class CBlockedIntQueue
        {
            public void clear()
            {
                lock (_mutex)
                {
                    while (_queue.TryTake(out _))
                    {
                        // Intentionally empty
                    }
                }
            }

            public int pop()
            {
                lock (_mutex)
                {
                    return _queue.Take();
                }
            }

            public void push(int value)
            {
                lock (_mutex)
                {
                    _queue.Add(value);
                }
            }

            private readonly object _mutex = new object();
            private readonly BlockingCollection<int> _queue = new BlockingCollection<int>(new ConcurrentQueue<int>());
        }

        private struct WordEntry
        {
            public int word_id_;
            public long offset_;
            public long end_offset_;
            public int capacity_;
            public int is_dense_;

            public int tf;
            public long alias_offset_;
            public long alias_end_offset_;
            public int alias_capacity_;
            public int is_alias_dense_;
        }

        private sealed class LDAModelBlock
        {
            public LDAModelBlock() => throw null;

            public hybrid_map get_row(int word_id, ref int external_buf) => throw null;
            public hybrid_alias_map get_alias_row(int word_id) => throw null;
            public void SetWordInfo(int word_id, int nonzero_num, bool fullSparse) => throw null;

            public void Clear() => throw null;
            public void Init(int num_vocabs, int num_topics) => throw null;
            public void Init(int num_vocabs, int num_topics, long nonzero_num) => throw null;
            public void Init(int num_vocabs, int num_topics, long mem_block_size, long alias_mem_block_size) => throw null;

            public void InitFromDataBlock(LDADataBlock data_block, int num_vocabs, int num_topics);

            public void GetModelStat(ref long mem_block_size, ref long alias_mem_block_size);
        }

        internal sealed class LdaEngine
        {
        }
    }
#pragma warning restore MSML_GeneralName // This name should be PascalCased
#pragma warning restore MSML_PrivateFieldName // Private field name not in: _camelCase format
#pragma warning restore MSML_ParameterLocalVarName // Parameter or local variable name not standard

    internal static class LdaInterface
    {
        public sealed class SafeLdaEngineHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeLdaEngineHandle()
                : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                DestroyEngine(handle);
                return true;
            }
        }

        private const string NativePath = "LdaNative";
        [DllImport(NativePath), SuppressUnmanagedCodeSecurity]
        internal static extern SafeLdaEngineHandle CreateEngine(int numTopic, int numVocab, float alphaSum, float beta, int numIter,
            int likelihoodInterval, int numThread, int mhstep, int maxDocToken);

        [DllImport(NativePath), SuppressUnmanagedCodeSecurity]
        internal static extern void AllocateModelMemory(SafeLdaEngineHandle engine, int numTopic, int numVocab, long tableSize, long aliasTableSize);

        [DllImport(NativePath), SuppressUnmanagedCodeSecurity]
        internal static extern void AllocateDataMemory(SafeLdaEngineHandle engine, int docNum, long corpusSize);

        [DllImport(NativePath, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity]
        internal static extern void Train(SafeLdaEngineHandle engine, string trainOutput);

        [DllImport(NativePath), SuppressUnmanagedCodeSecurity]
        internal static extern void GetModelStat(SafeLdaEngineHandle engine, out long memBlockSize, out long aliasMemBlockSize);

        [DllImport(NativePath), SuppressUnmanagedCodeSecurity]
        internal static extern void Test(SafeLdaEngineHandle engine, int numBurninIter, float[] pLogLikelihood);

        [DllImport(NativePath), SuppressUnmanagedCodeSecurity]
        internal static extern void CleanData(SafeLdaEngineHandle engine);

        [DllImport(NativePath), SuppressUnmanagedCodeSecurity]
        internal static extern void CleanModel(SafeLdaEngineHandle engine);

        [DllImport(NativePath), SuppressUnmanagedCodeSecurity]
        private static extern void DestroyEngine(IntPtr engine);

        [DllImport(NativePath), SuppressUnmanagedCodeSecurity]
        internal static extern void GetWordTopic(SafeLdaEngineHandle engine, int wordId, int[] pTopic, int[] pProb, ref int length);

        [DllImport(NativePath), SuppressUnmanagedCodeSecurity]
        internal static extern void SetWordTopic(SafeLdaEngineHandle engine, int wordId, int[] pTopic, int[] pProb, int length);

        [DllImport(NativePath), SuppressUnmanagedCodeSecurity]
        internal static extern void SetAlphaSum(SafeLdaEngineHandle engine, float avgDocLength);

        [DllImport(NativePath), SuppressUnmanagedCodeSecurity]
        internal static extern int FeedInData(SafeLdaEngineHandle engine, int[] termId, int[] termFreq, int termNum, int numVocab);

        [DllImport(NativePath), SuppressUnmanagedCodeSecurity]
        internal static extern int FeedInDataDense(SafeLdaEngineHandle engine, int[] termFreq, int termNum, int numVocab);

        [DllImport(NativePath), SuppressUnmanagedCodeSecurity]
        internal static extern void GetDocTopic(SafeLdaEngineHandle engine, int docId, int[] pTopic, int[] pProb, ref int numTopicReturn);

        [DllImport(NativePath), SuppressUnmanagedCodeSecurity]
        internal static extern void GetTopicSummary(SafeLdaEngineHandle engine, int topicId, int[] pWords, float[] pProb, ref int numTopicReturn);

        [DllImport(NativePath), SuppressUnmanagedCodeSecurity]
        internal static extern void TestOneDoc(SafeLdaEngineHandle engine, int[] termId, int[] termFreq, int termNum, int[] pTopics, int[] pProbs, ref int numTopicsMax, int numBurnIter, bool reset);

        [DllImport(NativePath), SuppressUnmanagedCodeSecurity]
        internal static extern void TestOneDocDense(SafeLdaEngineHandle engine, int[] termFreq, int termNum, int[] pTopics, int[] pProbs, ref int numTopicsMax, int numBurninIter, bool reset);

        [DllImport(NativePath), SuppressUnmanagedCodeSecurity]
        internal static extern void InitializeBeforeTrain(SafeLdaEngineHandle engine);

        [DllImport(NativePath), SuppressUnmanagedCodeSecurity]
        internal static extern void InitializeBeforeTest(SafeLdaEngineHandle engine);
    }

    internal sealed class LdaSingleBox : IDisposable
    {
        private LdaInterface.SafeLdaEngineHandle _engine;
        private bool _isDisposed;
        private int[] _topics;
        private int[] _probabilities;
        private int[] _summaryTerm;
        private float[] _summaryTermProb;
        private readonly int _likelihoodInterval;
        private readonly float _alpha;
        private readonly float _beta;
        private readonly int _mhStep;
        private readonly int _numThread;
        private readonly int _numSummaryTerms;
        private readonly bool _denseOutput;

        public readonly int NumTopic;
        public readonly int NumVocab;
        public LdaSingleBox(int numTopic, int numVocab, float alpha,
                            float beta, int numIter, int likelihoodInterval, int numThread,
                            int mhstep, int numSummaryTerms, bool denseOutput, int maxDocToken)
        {
            NumTopic = numTopic;
            NumVocab = numVocab;
            _alpha = alpha;
            _beta = beta;
            _mhStep = mhstep;
            _numSummaryTerms = numSummaryTerms;
            _denseOutput = denseOutput;
            _likelihoodInterval = likelihoodInterval;
            _numThread = numThread;

            _topics = new int[numTopic];
            _probabilities = new int[numTopic];

            _summaryTerm = new int[_numSummaryTerms];
            _summaryTermProb = new float[_numSummaryTerms];

            _engine = LdaInterface.CreateEngine(numTopic, numVocab, alpha, beta, numIter, likelihoodInterval, numThread, mhstep, maxDocToken);
        }

        public void AllocateModelMemory(int numTopic, int numVocab, long tableSize, long aliasTableSize)
        {
            Contracts.Check(numTopic >= 0);
            Contracts.Check(numVocab >= 0);
            Contracts.Check(tableSize >= 0);
            Contracts.Check(aliasTableSize >= 0);
            LdaInterface.AllocateModelMemory(_engine, numVocab, numTopic, tableSize, aliasTableSize);
        }

        public void AllocateDataMemory(int docNum, long corpusSize)
        {
            Contracts.Check(docNum >= 0);
            Contracts.Check(corpusSize >= 0);
            LdaInterface.AllocateDataMemory(_engine, docNum, corpusSize);
        }

        public void Train(string trainOutput)
        {
            if (string.IsNullOrWhiteSpace(trainOutput))
                LdaInterface.Train(_engine, null);
            else
                LdaInterface.Train(_engine, trainOutput);
        }

        public void GetModelStat(out long memBlockSize, out long aliasMemBlockSize)
        {
            LdaInterface.GetModelStat(_engine, out memBlockSize, out aliasMemBlockSize);
        }

        public void Test(int numBurninIter, float[] logLikelihood)
        {
            Contracts.Check(numBurninIter >= 0);
            var pLogLikelihood = new float[numBurninIter];
            LdaInterface.Test(_engine, numBurninIter, pLogLikelihood);
            logLikelihood = pLogLikelihood.Select(item => (float)item).ToArray();
        }

        public void CleanData()
        {
            LdaInterface.CleanData(_engine);
        }

        public void CleanModel()
        {
            LdaInterface.CleanModel(_engine);
        }

        public void CopyModel(LdaSingleBox trainer, int wordId)
        {
            int length = NumTopic;
            LdaInterface.GetWordTopic(trainer._engine, wordId, _topics, _probabilities, ref length);
            LdaInterface.SetWordTopic(_engine, wordId, _topics, _probabilities, length);
        }

        public void SetAlphaSum(float averageDocLength)
        {
            LdaInterface.SetAlphaSum(_engine, averageDocLength);
        }

        public int LoadDoc(ReadOnlySpan<int> termID, ReadOnlySpan<double> termVal, int termNum, int numVocab)
        {
            Contracts.Check(numVocab == NumVocab);
            Contracts.Check(termNum > 0);
            Contracts.Check(termID.Length >= termNum);
            Contracts.Check(termVal.Length >= termNum);

            int[] pID = new int[termNum];
            int[] pVal = new int[termVal.Length];
            for (int i = 0; i < termVal.Length; i++)
                pVal[i] = (int)termVal[i];
            termID.Slice(0, termNum).CopyTo(pID);
            return LdaInterface.FeedInData(_engine, pID, pVal, termNum, NumVocab);
        }

        public int LoadDocDense(ReadOnlySpan<double> termVal, int termNum, int numVocab)
        {
            Contracts.Check(numVocab == NumVocab);
            Contracts.Check(termNum > 0);

            Contracts.Check(termVal.Length >= termNum);

            int[] pID = new int[termNum];
            int[] pVal = new int[termVal.Length];
            for (int i = 0; i < termVal.Length; i++)
                pVal[i] = (int)termVal[i];
            return LdaInterface.FeedInDataDense(_engine, pVal, termNum, NumVocab);
        }

        public List<KeyValuePair<int, float>> GetDocTopicVector(int docID)
        {
            int numTopicReturn = NumTopic;
            LdaInterface.GetDocTopic(_engine, docID, _topics, _probabilities, ref numTopicReturn);
            var topicRet = new List<KeyValuePair<int, float>>();
            int currentTopic = 0;
            for (int i = 0; i < numTopicReturn; i++)
            {
                if (_denseOutput)
                {
                    while (currentTopic < _topics[i])
                    {
                        //use a value to smooth the count so that we get dense output on each topic
                        //the smooth value is usually set to 0.1
                        topicRet.Add(new KeyValuePair<int, float>(currentTopic, (float)_alpha));
                        currentTopic++;
                    }
                    topicRet.Add(new KeyValuePair<int, float>(_topics[i], _probabilities[i] + (float)_alpha));
                    currentTopic++;
                }
                else
                {
                    topicRet.Add(new KeyValuePair<int, float>(_topics[i], (float)_probabilities[i]));
                }
            }

            if (_denseOutput)
            {
                while (currentTopic < NumTopic)
                {
                    topicRet.Add(new KeyValuePair<int, float>(currentTopic, (float)_alpha));
                    currentTopic++;
                }
            }
            return topicRet;
        }

        public List<KeyValuePair<int, float>> TestDoc(ReadOnlySpan<int> termID, ReadOnlySpan<double> termVal, int termNum, int numBurninIter, bool reset)
        {
            Contracts.Check(termNum > 0);
            Contracts.Check(termVal.Length >= termNum);
            Contracts.Check(termID.Length >= termNum);

            int[] pID = new int[termNum];
            int[] pVal = new int[termVal.Length];
            for (int i = 0; i < termVal.Length; i++)
                pVal[i] = (int)termVal[i];
            int[] pTopic = new int[NumTopic];
            int[] pProb = new int[NumTopic];
            termID.Slice(0, termNum).CopyTo(pID);

            int numTopicReturn = NumTopic;

            LdaInterface.TestOneDoc(_engine, pID, pVal, termNum, pTopic, pProb, ref numTopicReturn, numBurninIter, reset);

            // PREfast suspects that the value of numTopicReturn could be changed in _engine->TestOneDoc, which might result in read overrun in the following loop.
            if (numTopicReturn > NumTopic)
            {
                Contracts.Check(false);
                numTopicReturn = NumTopic;
            }

            var topicRet = new List<KeyValuePair<int, float>>();
            for (int i = 0; i < numTopicReturn; i++)
                topicRet.Add(new KeyValuePair<int, float>(pTopic[i], (float)pProb[i]));
            return topicRet;
        }

        public List<KeyValuePair<int, float>> TestDocDense(ReadOnlySpan<double> termVal, int termNum, int numBurninIter, bool reset)
        {
            Contracts.Check(termNum > 0);
            Contracts.Check(numBurninIter > 0);
            Contracts.Check(termVal.Length >= termNum);
            int[] pVal = new int[termVal.Length];
            for (int i = 0; i < termVal.Length; i++)
                pVal[i] = (int)termVal[i];
            int[] pTopic = new int[NumTopic];
            int[] pProb = new int[NumTopic];

            int numTopicReturn = NumTopic;

            // There are two versions of TestOneDoc interfaces
            // (1) TestOneDoc
            // (2) TestOneDocRestart
            // The second one is the same as the first one except that it will reset
            // the states of the internal random number generator, so that it yields reproducable results for the same input
            LdaInterface.TestOneDocDense(_engine, pVal, termNum, pTopic, pProb, ref numTopicReturn, numBurninIter, reset);

            // PREfast suspects that the value of numTopicReturn could be changed in _engine->TestOneDoc, which might result in read overrun in the following loop.
            if (numTopicReturn > NumTopic)
            {
                Contracts.Check(false);
                numTopicReturn = NumTopic;
            }

            var topicRet = new List<KeyValuePair<int, float>>();
            for (int i = 0; i < numTopicReturn; i++)
                topicRet.Add(new KeyValuePair<int, float>(pTopic[i], (float)pProb[i]));
            return topicRet;
        }

        public void InitializeBeforeTrain()
        {
            LdaInterface.InitializeBeforeTrain(_engine);
        }

        public void InitializeBeforeTest()
        {
            LdaInterface.InitializeBeforeTest(_engine);
        }

        public KeyValuePair<int, int>[] GetModel(int wordId)
        {
            int length = NumTopic;
            LdaInterface.GetWordTopic(_engine, wordId, _topics, _probabilities, ref length);
            var wordTopicVector = new KeyValuePair<int, int>[length];

            for (int i = 0; i < length; i++)
                wordTopicVector[i] = new KeyValuePair<int, int>(_topics[i], _probabilities[i]);
            return wordTopicVector;
        }

        public KeyValuePair<int, float>[] GetTopicSummary(int topicId)
        {
            int length = _numSummaryTerms;
            LdaInterface.GetTopicSummary(_engine, topicId, _summaryTerm, _summaryTermProb, ref length);
            var topicSummary = new KeyValuePair<int, float>[length];

            for (int i = 0; i < length; i++)
                topicSummary[i] = new KeyValuePair<int, float>(_summaryTerm[i], _summaryTermProb[i]);
            return topicSummary;
        }

        public void SetModel(int termID, int[] topicID, int[] topicProb, int topicNum)
        {
            Contracts.Check(termID >= 0);
            Contracts.Check(topicNum <= NumTopic);
            Array.Copy(topicID, _topics, topicNum);
            Array.Copy(topicProb, _probabilities, topicNum);
            LdaInterface.SetWordTopic(_engine, termID, _topics, _probabilities, topicNum);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            _engine.Dispose();
        }
    }
}
