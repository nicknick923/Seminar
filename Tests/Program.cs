using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Logic;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            TestAdd(0, 0);
            TestAdd(0, 1);
            TestAdd(1, 0);
            TestAdd(1, 1);
            Console.WriteLine("Done");
            Console.ReadLine();
        }

        public static void TestAdd(decimal value1, decimal value2)
        {
            Assert(MathClass.Add(value1, value2), value1 + value2);
        }

        public static void Assert(object object1, object object2,
            [CallerFilePath] string callingFile = null,
            [CallerMemberName] string callingMember = null,
            [CallerLineNumber] int callingLine = 0)
        {
            if (!Equals(object1, object2))
            {
                StackTrace st = new StackTrace();
                Console.WriteLine($"{callingFile}:{callingLine}: {callingMember} failed to assert equals: {object1.ToString()} {object2.ToString()}");
                Console.WriteLine(st.ToString());
            }
        }
    }
}
