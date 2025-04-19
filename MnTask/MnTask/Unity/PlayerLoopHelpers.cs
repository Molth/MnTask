#if UNITY_2021_3_OR_NEWER
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

#pragma warning disable CS1591
#pragma warning disable CS8618
#pragma warning disable CS8632

// ReSharper disable ALL

namespace Erinn
{
    internal static class PlayerLoopHelpers
    {
        public static ConcurrentQueue<Action?> SwitchToMainThreadUpdateQueue;
        public static ConcurrentQueue<Action?> SwitchToMainThreadLateUpdateQueue;
        public static ConcurrentQueue<Action?> SwitchToMainThreadFixedUpdateQueue;

        public static MnTaskQueue TimeQueue;
        public static MnTaskQueue UnscaledTimeQueue;

        public static MnTaskQueue UpdateQueue;
        public static MnTaskQueue LateUpdateQueue;
        public static MnTaskQueue FixedUpdateQueue;

        public static ulong UpdateFrames;
        public static ulong LateUpdateFrames;
        public static ulong FixedUpdateFrames;

        public const int MN_TASK_QUEUE_INITIAL_CAPACITY = 16;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            SwitchToMainThreadUpdateQueue = new ConcurrentQueue<Action?>();
            SwitchToMainThreadLateUpdateQueue = new ConcurrentQueue<Action?>();
            SwitchToMainThreadFixedUpdateQueue = new ConcurrentQueue<Action?>();

            TimeQueue = new MnTaskQueue(MN_TASK_QUEUE_INITIAL_CAPACITY);
            UnscaledTimeQueue = new MnTaskQueue(MN_TASK_QUEUE_INITIAL_CAPACITY);

            UpdateQueue = new MnTaskQueue(MN_TASK_QUEUE_INITIAL_CAPACITY);
            LateUpdateQueue = new MnTaskQueue(MN_TASK_QUEUE_INITIAL_CAPACITY);
            FixedUpdateQueue = new MnTaskQueue(MN_TASK_QUEUE_INITIAL_CAPACITY);

            UpdateFrames = 0;
            LateUpdateFrames = 0;
            FixedUpdateFrames = 0;

            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            AddToPlayerLoop(Update, typeof(MnTask), ref playerLoop, typeof(Update));
            AddToPlayerLoop(LateUpdate, typeof(MnTask), ref playerLoop, typeof(PreLateUpdate));
            AddToPlayerLoop(FixedUpdate, typeof(MnTask), ref playerLoop, typeof(FixedUpdate));

            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AddToPlayerLoop(PlayerLoopSystem.UpdateFunction function, Type ownerType, ref PlayerLoopSystem playerLoop, Type playerLoopSystemType)
        {
            if (playerLoop.type == playerLoopSystemType)
            {
                if (Array.FindIndex(playerLoop.subSystemList, s => s.updateDelegate == function) != -1)
                    return true;
                playerLoop.subSystemList ??= Array.Empty<PlayerLoopSystem>();
                var system = new PlayerLoopSystem { type = ownerType, updateDelegate = function };
                var tempQualifier = playerLoop.subSystemList;
                var length = tempQualifier.Length;
                Array.Resize(ref tempQualifier, length + 1);
                tempQualifier[length] = system;
                playerLoop.subSystemList = tempQualifier;
                return true;
            }

            if (playerLoop.subSystemList != null)
            {
                for (var i = 0; i < playerLoop.subSystemList.Length; ++i)
                {
                    if (AddToPlayerLoop(function, ownerType, ref playerLoop.subSystemList[i], playerLoopSystemType))
                        return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToNanoSeconds(float seconds) => (ulong)(seconds * 1_000_000_000.0f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Update()
        {
            while (SwitchToMainThreadUpdateQueue.TryDequeue(out var callback))
            {
                Debug.Assert(callback != null);
                callback?.Invoke();
            }

            TimeQueue.Update(ToNanoSeconds(Time.time));

            UnscaledTimeQueue.Update(ToNanoSeconds(Time.unscaledTime));

            UpdateQueue.Update(++UpdateFrames);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LateUpdate()
        {
            while (SwitchToMainThreadLateUpdateQueue.TryDequeue(out var callback))
            {
                Debug.Assert(callback != null);
                callback?.Invoke();
            }

            LateUpdateQueue.Update(++LateUpdateFrames);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FixedUpdate()
        {
            while (SwitchToMainThreadFixedUpdateQueue.TryDequeue(out var callback))
            {
                Debug.Assert(callback != null);
                callback?.Invoke();
            }

            FixedUpdateQueue.Update(++FixedUpdateFrames);
        }
    }
}
#endif