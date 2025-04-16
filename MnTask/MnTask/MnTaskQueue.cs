using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable CS1591
#pragma warning disable CS8632

// ReSharper disable ALL

namespace Erinn
{
    public sealed class MnTaskQueue
    {
        private MnTaskPromise?[] _nodes;
        private readonly Stack<MnTaskPromise> _freeList;
        private int _size;
        private uint _sequenceNumber;
        private float _timestamp;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MnTaskQueue(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "MustBeNonNegative");

            if (capacity < 4)
                capacity = 4;

            _nodes = new MnTaskPromise[capacity];
            _freeList = new Stack<MnTaskPromise>(capacity);
            _size = 0;
            _sequenceNumber = 0;
            _timestamp = 0.0f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MnTask Create(float delay)
        {
            if (!_freeList.TryPop(out var promise))
                promise = new MnTaskPromise(this);

            promise.SequenceNumber = ++_sequenceNumber;
            promise.State = MnTaskResult.Pending;

            return new MnTask(delay, _sequenceNumber, promise);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(float timestamp)
        {
            _timestamp = timestamp;

            while (_size != 0)
            {
                var node = _nodes[0];

                if (node!.Timestamp > timestamp)
                {
                    for (; node!.Callback == null; node = _nodes[0])
                    {
                        RemoveRootNode();
                        _freeList.Push(node);

                        Debug.Assert(node.State == MnTaskResult.Canceled || node.State == MnTaskResult.Stopped);

                        if (_size == 0)
                            break;
                    }

                    break;
                }

                RemoveRootNode();
                _freeList.Push(node);

                Debug.Assert(node.State == MnTaskResult.Running);

                node.State = MnTaskResult.Success;

                var callback = node.Callback;
                node.Callback = null;

                Debug.Assert(callback != null);
                callback?.Invoke();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            for (var i = 0; i < _size; ++i)
            {
                var node = _nodes[i]!;

                node.State = MnTaskResult.Fault;
                node.Callback = null;

                _freeList.Push(node);
            }

            Array.Clear(_nodes, 0, _size);
            _size = 0;
        }

        [DebuggerHidden]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Enqueue(float delay, uint sequenceNumber, MnTaskPromise promise, Action callback)
        {
            promise.Timestamp = _timestamp + delay;
            promise.State = MnTaskResult.Running;
            promise.Callback = callback;

            var size = _size;
            if (_nodes.Length == size)
                Grow(size + 1);
            _size = size + 1;
            MoveUp(promise, size);
        }

        [DebuggerHidden]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Push(MnTaskPromise promise) => _freeList.Push(promise);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Grow(int capacity)
        {
            var newCapacity = 2 * _nodes.Length;
            if ((uint)newCapacity > 2147483591)
                newCapacity = 2147483591;
            var newSize = Math.Max(newCapacity, _nodes.Length + 4);
            if (newSize < capacity)
                newSize = capacity;
            var nodes = new MnTaskPromise[newSize];
            Array.Copy(_nodes, 0, nodes, 0, _size);
            _nodes = nodes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MoveUp(MnTaskPromise? node, int nodeIndex)
        {
            var nodes = _nodes;
            int parentIndex;
            for (; nodeIndex > 0; nodeIndex = parentIndex)
            {
                parentIndex = (nodeIndex - 1) >> 2;
                var tuple = nodes[parentIndex]!;
                if (node!.CompareTo(tuple) < 0)
                    nodes[nodeIndex] = tuple;
                else
                    break;
            }

            nodes[nodeIndex] = node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MoveDown(MnTaskPromise? node, int nodeIndex)
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

                if (node!.CompareTo(valueTuple) > 0)
                    nodes[nodeIndex] = valueTuple;
                else
                    break;
            }

            nodes[nodeIndex] = node;
        }
    }
}