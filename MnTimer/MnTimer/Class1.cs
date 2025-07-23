using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
#if NET7_0_OR_GREATER
using System.Numerics;
#endif

#pragma warning disable CS1591
#pragma warning disable CS8632

namespace Erinn
{
    public interface IMnTimer
    {
        void OnComplete();
    }

    internal static class MnTimerHelper<T> where T : IMnTimer
    {
        public static void OnComplete(T? arg) => arg?.OnComplete();
    }

    public readonly struct MnTimer
    {
        internal readonly ulong SequenceNumber;
        internal readonly IMnTimerPromise? Promise;

        internal MnTimer(ulong sequenceNumber, IMnTimerPromise promise)
        {
            SequenceNumber = sequenceNumber;
            Promise = promise;
        }

        public void Cancel()
        {
            var promise = Promise;
            if (promise != null && promise.SequenceNumber == SequenceNumber)
                promise.OnComplete();
        }

        public void Stop()
        {
            var promise = Promise;
            if (promise != null && promise.SequenceNumber == SequenceNumber)
                promise.Reset();
        }
    }

    internal interface IMnTimerPromise
    {
        ulong Timestamp { get; set; }
        ulong SequenceNumber { get; set; }
        MnTimerState State { get; set; }
        Type Type { get; }
        bool IsCreated { get; }

        void OnComplete();
        void Reset();

        int CompareTo(IMnTimerPromise other)
        {
            var timestampComparison = Timestamp.CompareTo(other.Timestamp);
            return timestampComparison != 0 ? timestampComparison : SequenceNumber.CompareTo(other.SequenceNumber);
        }
    }

    internal enum MnTimerState
    {
        None,
        Running
    }

    internal sealed unsafe class MnTimerPromise<T> : IMnTimerPromise
    {
        public delegate* managed<T?, void> FunctionPointer;
        public T? Arg;
        public ulong Timestamp { get; set; }
        public ulong SequenceNumber { get; set; }
        public MnTimerState State { get; set; }
        public Type Type => !typeof(T).IsValueType ? typeof(object) : typeof(T);
        public bool IsCreated => FunctionPointer != null;

        public void OnComplete()
        {
            var functionPointer = FunctionPointer;
            var arg = Arg;
            FunctionPointer = null;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                Arg = default;
            State = MnTimerState.None;
            if (functionPointer != null)
                functionPointer(arg);
        }

        public void Reset()
        {
            FunctionPointer = null;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                Arg = default;
            State = MnTimerState.None;
        }
    }

    internal static class ThrowHelper
    {
        /// <summary>Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is negative.</summary>
        /// <param name="value">The argument to validate as non-negative.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="value" /> corresponds.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNegative<T>(T value, string? paramName) where T : unmanaged,
#if NET7_0_OR_GREATER
            ISignedNumber<T>
#else
            IComparable<T>
#endif
        {
#if NET7_0_OR_GREATER
            if (T.IsNegative(value))
#else
            if (value.CompareTo(default) < 0)
#endif
                throw new ArgumentOutOfRangeException(paramName, value, "MustBeNonNegative");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNotStatic(MethodInfo methodInfo)
        {
            if (!methodInfo.IsStatic)
                throw new NotSupportedException("MustBeStatic");
        }
    }

    public sealed unsafe class MnTimerQueue
    {
        private IMnTimerPromise?[] _nodes;
        private readonly Dictionary<Type, Queue<IMnTimerPromise>> _objectPool;
        private int _size;
        private uint _sequenceNumber;
        private ulong _timestamp;

        public MnTimerQueue(int capacity)
        {
            ThrowHelper.ThrowIfNegative(capacity, nameof(capacity));

            if (capacity < 4)
                capacity = 4;

            _nodes = new IMnTimerPromise?[capacity];
            _objectPool = new Dictionary<Type, Queue<IMnTimerPromise>>();
            _size = 0;
            _sequenceNumber = 0;
            _timestamp = 0;
        }

        private IMnTimerPromise Rent<T>()
        {
            if (!_objectPool.TryGetValue(typeof(T), out var queue))
            {
                queue = new Queue<IMnTimerPromise>(16);
                _objectPool[typeof(T)] = queue;
            }

            if (!queue.TryDequeue(out var item))
                item = new MnTimerPromise<T>();

            return item;
        }

        private void Return(IMnTimerPromise promise)
        {
            var type = promise.Type;
            if (type != null && _objectPool.TryGetValue(type, out var queue))
                queue.Enqueue(promise);
        }

        public MnTimer Create<T>(ulong delay, Action<T?> functionPointer, T? arg)
        {
            ThrowHelper.ThrowIfNotStatic(functionPointer.Method);

            return Create(delay, (delegate* managed<T?, void>)functionPointer.Method.MethodHandle.GetFunctionPointer(), arg);
        }

        public MnTimer Create<T>(ulong delay, T? arg) where T : IMnTimer => Create(delay, &MnTimerHelper<T>.OnComplete, arg);

        public MnTimer Create<T>(ulong delay, delegate* managed<T?, void> functionPointer, T? arg)
        {
            IMnTimerPromise promise;
            if (!typeof(T).IsValueType)
            {
                var item = (MnTimerPromise<object>)Rent<object>();
                item.FunctionPointer = (delegate* managed<object?, void>)functionPointer;
                item.Arg = arg;
                promise = item;
            }
            else
            {
                var item = (MnTimerPromise<T>)Rent<T>();
                item.FunctionPointer = functionPointer;
                item.Arg = arg;
                promise = item;
            }

            promise.Timestamp = _timestamp + delay;
            promise.SequenceNumber = ++_sequenceNumber;
            promise.State = MnTimerState.Running;
            var size = _size;
            if (_nodes.Length == size)
                Grow(size + 1);
            _size = size + 1;
            MoveUp(promise, size);
            return new MnTimer(_sequenceNumber, promise);
        }

        public void Update(ulong timestamp)
        {
            _timestamp = timestamp;
            while (_size != 0)
            {
                var node = _nodes[0];
                if (node!.Timestamp > timestamp)
                {
                    for (; !node!.IsCreated; node = _nodes[0])
                    {
                        RemoveRootNode();
                        Return(node);
                        node.Reset();
                        if (_size == 0)
                            break;
                    }

                    break;
                }

                RemoveRootNode();
                Return(node);
                node.OnComplete();
            }
        }

        public void Clear()
        {
            for (var i = 0; i < _size; ++i)
            {
                var node = _nodes[i]!;
                node.Reset();
                Return(node);
            }

            Array.Clear(_nodes, 0, _size);
            _size = 0;
        }

        private void Grow(int capacity)
        {
            var newCapacity = 2 * _nodes.Length;
            if ((uint)newCapacity > 2147483591)
                newCapacity = 2147483591;
            var newSize = Math.Max(newCapacity, _nodes.Length + 4);
            if (newSize < capacity)
                newSize = capacity;
            var nodes = new IMnTimerPromise[newSize];
            Array.Copy(_nodes, 0, nodes, 0, _size);
            _nodes = nodes;
        }

        private void RemoveRootNode()
        {
            var index = --_size;
            if (index > 0)
            {
                var node = _nodes[index];
                MoveDown(node!, 0);
            }

            _nodes[index] = null;
        }

        private void MoveUp(IMnTimerPromise node, int nodeIndex)
        {
            var nodes = _nodes;
            int parentIndex;
            for (; nodeIndex > 0; nodeIndex = parentIndex)
            {
                parentIndex = (nodeIndex - 1) >> 2;
                var tuple = nodes[parentIndex]!;
                if (node.CompareTo(tuple) < 0)
                    nodes[nodeIndex] = tuple;
                else
                    break;
            }

            nodes[nodeIndex] = node;
        }

        private void MoveDown(IMnTimerPromise node, int nodeIndex)
        {
            var nodes = _nodes;
            int firstChildIndex;
            int num1;
            for (var size = _size; (firstChildIndex = (nodeIndex << 2) + 1) < size; nodeIndex = num1)
            {
                var valueTuple = nodes[firstChildIndex]!;
                num1 = firstChildIndex;
                var num2 = Math.Min(firstChildIndex + 4, size);
                while (++firstChildIndex < num2)
                {
                    var tuple = nodes[firstChildIndex]!;
                    if (tuple.CompareTo(valueTuple) < 0)
                    {
                        valueTuple = tuple;
                        num1 = firstChildIndex;
                    }
                }

                if (node.CompareTo(valueTuple) > 0)
                    nodes[nodeIndex] = valueTuple;
                else
                    break;
            }

            nodes[nodeIndex] = node;
        }
    }
}