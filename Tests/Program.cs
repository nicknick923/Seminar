using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Logic;
using static Mutator.Mutator;

namespace Tests
{
    class Program
    {
        private const string FilePath = @"C:\Users\Nick\source\repos\Seminar\Logic\MathClass.cs";
        static TextReader Input = Console.In;
        static TextWriter RegularOutput = Console.Out;
        static TextWriter AssertOutput = Console.Out;
        static void Main(string[] args)
        {
            AssertOutput = new StringWriter();
            TestAddMethod();
            TestSubtractMethod();
            Input.ReadLine();
        }

        private static void TestAddMethod()
        {
            MethodInfo originalAddMethodInfo = typeof(MathClass).GetTypeInfo().GetMethod(nameof(MathClass.Add));
            MethodInput[] inputs = new MethodInput[]
            {
                new MethodInput(null, 0m, 0m),
                new MethodInput(null, 0m, 1m),
                new MethodInput(null, 1m, 0m),
                new MethodInput(null, 1m, 1m),
                new MethodInput(null, 2m, 0m),
                new MethodInput(null, 2m, 1m),
                new MethodInput(null, 2m, 2m),
                new MethodInput(null, 2m, 3m)
            };
            TestAnyMethod(originalAddMethodInfo, inputs, Mutate(FilePath, Mutatations.Add, Mutatations.Subract));
        }

        private static void TestSubtractMethod()
        {
            MethodInfo originalSubtractMethodInfo = typeof(MathClass).GetTypeInfo().GetMethod(nameof(MathClass.Subtract));
            MethodInput[] inputs = new MethodInput[]
            {
                new MethodInput(null, 0m, 0m),
                new MethodInput(null, 0m, 1m),
                new MethodInput(null, 1m, 0m),
                new MethodInput(null, 1m, 1m),
                new MethodInput(null, 2m, 0m),
                new MethodInput(null, 2m, 1m),
                new MethodInput(null, 2m, 2m),
                new MethodInput(null, 2m, 3m)
            };
            TestAnyMethod(originalSubtractMethodInfo, inputs, Mutate(FilePath, Mutatations.Subract, Mutatations.Add));
        }

        private static void TestAnyMethod(MethodInfo originalMethodInfo, MethodInput[] inputs, IEnumerable<MutationResult> mutationResults)
        {
            bool failedOverall = false;
            StringBuilder mainStringBuilder = new StringBuilder($"Method '{originalMethodInfo.Name}' may not have passed testing:");
            mainStringBuilder.AppendLine();
            foreach (MutationResult mutationResult in mutationResults)
            {
                bool failed = false;
                StringBuilder mutationResultStringBuilder = new StringBuilder($"\t{mutationResult.Summary} | Cases to Investigate:");
                mutationResultStringBuilder.AppendLine();
                foreach (MethodInput methodInput in inputs)
                {
                    MutationResultMethodTestResult mutationResultMethodTestResult = TestMutatedMethod(originalMethodInfo, mutationResult.Assembly, methodInput);
                    if (mutationResultMethodTestResult.Assert)
                    {
                        mutationResultStringBuilder.AppendLine($"\t\t{mutationResultMethodTestResult.ToString()}");
                        failed = true;
                    }
                }
                if (failed)
                {
                    mainStringBuilder.AppendLine(mutationResultStringBuilder.ToString());
                }
            }
            //if (failedOverall)
            {
                RegularOutput.WriteLine(mainStringBuilder.ToString());
            }
            //else
            {
                RegularOutput.WriteLine($"Method '{originalMethodInfo.Name}' passed testing:");
            }
        }

        private class MethodInput
        {
            public readonly object Instance;
            public readonly object[] Parameters;

            public MethodInput(object instance, params object[] parametrs)
            {
                Instance = instance;
                Parameters = parametrs;
            }
        }

        private class MutationResultMethodTestResult
        {
            public readonly MethodInput MethodInput;
            public readonly object OriginalResult;
            public readonly object MutatedResult;
            public readonly Exception OriginalException;
            public readonly Exception MutatedException;
            public MutationResultMethodTestResult(MethodInput input, object originalResult, object mutatedResult, Exception originalException, Exception mutatedException)
            {
                MethodInput = input;
                OriginalResult = originalResult;
                MutatedResult = mutatedResult;
                OriginalException = originalException;
                MutatedException = mutatedException;
            }

            private bool _assert = false;
            private bool asserted = false;
            public bool Assert
            {
                get
                {
                    if (!asserted)
                    {
                        bool results = Assert(OriginalResult, MutatedResult);
                        bool exceptions = Assert(OriginalException, MutatedException);
                        _assert = results && exceptions;
                        asserted = true;
                    }
                    return _assert;
                }
            }
            public override string ToString()
            {
                List<string> parts = new List<string>();
                parts.Add($"Input: {string.Join(", ", MethodInput.Parameters)}");
                if (OriginalResult != null)
                    parts.Add($"Original: {OriginalResult.ToString()}");
                if (OriginalException != null)
                    parts.Add($"Original Exception: {OriginalException.ToString()}: {OriginalException.Message}");

                if (MutatedResult != null)
                    parts.Add($"Mutated: {MutatedResult.ToString()}");
                if (MutatedException != null)
                    parts.Add($"Mutated Exception: {MutatedException.ToString()}: {MutatedException.Message}");

                parts.Add($"Assert: {Assert}");
                return string.Join(" | ", parts);
            }
        }

        private static MutationResultMethodTestResult TestMutatedMethod(MethodInfo originalMethodInfo, Assembly mutatedAssembly, MethodInput methodInput)
        {
            TypeInfo mutatedTypeInfo = GetTypeInfoForClass(mutatedAssembly, originalMethodInfo.DeclaringType);
            MethodInfo mutatedMethodInfo = mutatedTypeInfo.GetMethod(originalMethodInfo.Name);

            object originalResult = null;
            Exception originalException = null;
            try
            {
                originalResult = originalMethodInfo.Invoke(methodInput.Instance, methodInput.Parameters);

            }
            catch (Exception e)
            {
                originalException = e;
            }

            object mutatedResult = null;
            Exception mutatedException = null;
            try
            {
                mutatedResult = mutatedMethodInfo.Invoke(methodInput.Instance, methodInput.Parameters);
            }
            catch (Exception e)
            {
                mutatedException = e;
            }
            Assert(originalResult, mutatedResult);
            Assert(originalException, mutatedException);
            return new MutationResultMethodTestResult(methodInput, originalResult, mutatedResult, originalException, mutatedException);
        }

        private static TypeInfo GetTypeInfoForClass(Assembly assembly, Type type)
        {
            return assembly.DefinedTypes.Where(t => t.FullName == type.FullName).Single();
        }

        public static bool Assert(object object1, object object2,
            [CallerFilePath] string callingFile = null,
            [CallerMemberName] string callingMember = null,
            [CallerLineNumber] int callingLine = 0)
        {
            if (Equals(object1, object2))
            {
                return true;
            }
            else
            {
                StackTrace st = new StackTrace();
                AssertOutput.WriteLine($"{callingFile}:{callingLine}: {callingMember} failed to assert equals: {object1?.ToString()} {object2?.ToString()}");
                AssertOutput.WriteLine(st.ToString());
                return false;
            }
        }
    }
}
