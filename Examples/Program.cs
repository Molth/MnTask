using System;
using System.Threading;
using Erinn;

namespace Examples
{
    internal struct TestStruct
    {
        public MnTimerQueue Queue;
        public TestClass Value;
    }

    internal class TestClass
    {
        public MnTimerQueue? Queue;
        public int Value;
    }

    internal struct TestStruct2 : IMnTimer
    {
        public int Value;

        void IMnTimer.OnComplete()
        {
            Console.WriteLine(DateTime.Now + " 1 " + Value);
        }

        public void OnComplete()
        {
            Console.WriteLine(DateTime.Now + " 2 " + Value);
        }
    }

    internal static unsafe class Program
    {
        private static void Main(string[] args)
        {
            Run();
        }

        private static void Run()
        {
            Console.WriteLine(DateTime.Now);
            var queue = new MnTimerQueue(16);
            queue.Update((ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds());
            var testClass = new TestClass { Queue = queue };
            var testStruct = new TestStruct { Queue = queue, Value = testClass };

            queue.Create((ulong)TimeSpan.FromSeconds(1).TotalMilliseconds, OnComplete, testStruct);
            queue.Create((ulong)TimeSpan.FromSeconds(1).TotalMilliseconds, &OnComplete, testClass);
            queue.Create((ulong)TimeSpan.FromSeconds(1).TotalMilliseconds, new TestStruct2 { Value = 1000 });

            while (true)
            {
                queue.Update((ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds());
                Thread.Sleep(100);
            }
        }

        private static void OnComplete(TestStruct value)
        {
            value.Value.Value++;
            value.Queue.Create((ulong)TimeSpan.FromSeconds(1).TotalMilliseconds, &OnComplete, value);
        }

        private static void OnComplete(TestClass? value)
        {
            if (value == null)
                return;
            Console.WriteLine(DateTime.Now + " " + value.Value);
            value.Queue?.Create((ulong)TimeSpan.FromSeconds(1).TotalMilliseconds, &OnComplete, value);
        }
    }
}