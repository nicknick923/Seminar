using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Logic;

namespace Tests
{
    class Program
    {
        static TextReader Input = Console.In;
        static TextWriter RegularOutput = Console.Out;
        static TextWriter AssertOutput = Console.Out;
        static void Main(string[] args)
        {
            TypeInfo typeInfo = typeof(MathClass).GetTypeInfo();
            MethodInfo originalAddMethodInfo = typeInfo.GetMethod(nameof(MathClass.Add));

            MethodInput[] inputs = new MethodInput[]
            {
                    new MethodInput(null, 0m, 0m),
                    new MethodInput(null, 0m, 1m),
                    new MethodInput(null, 1m, 0m),
                    new MethodInput(null, 1m, 1m)
            };

            if (Mutator.Mutator.MutateSingleFile(@"C:\Users\Nick\source\repos\Seminar\Logic\MathClass.cs",
                Mutator.Mutator.Mutatations.Add,
                Mutator.Mutator.Mutatations.Subract)
                .Any(mutatedAssembly =>
                {
                    return inputs.Any(input =>
                    {
                        return !TestMutatedMethod(originalAddMethodInfo, mutatedAssembly, input);
                    });
                }))
            {
                RegularOutput.WriteLine($"Method {originalAddMethodInfo.Name} passed testing");
            }
            else
            {
                RegularOutput.WriteLine($"Method {originalAddMethodInfo.Name} did not pass testing");
            }
            RegularOutput.WriteLine("Done");
            Input.ReadLine();
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

        private static bool TestMutatedMethod(MethodInfo originalMethodInfo, Assembly mutatedAssembly, MethodInput methodInput)
        {
            TypeInfo mutatedTypeInfo = GetTypeInfoForClass(mutatedAssembly, originalMethodInfo.DeclaringType);
            MethodInfo mutatedMethodInfo = mutatedTypeInfo.GetMethod(originalMethodInfo.Name);
            return TestMethod(originalMethodInfo, mutatedMethodInfo, methodInput.Instance, methodInput.Parameters);
        }

        private static TypeInfo GetTypeInfoForClass(Assembly assembly, Type type)
        {
            return assembly.DefinedTypes.Where(t => t.FullName == type.FullName).Single();
        }

        private static bool TestMethod(MethodInfo original, MethodInfo mutated, object instance, params object[] parameters)
        {
            object originalResult = null;
            Exception originalException = null;
            try
            {
                originalResult = original.Invoke(instance, parameters);

            }
            catch (Exception e)
            {
                originalException = e;
            }

            object mutatedResult = null;
            Exception mutatedException = null;
            try
            {
                mutatedResult = mutated.Invoke(instance, parameters);
            }
            catch (Exception e)
            {
                mutatedException = e;
            }
            bool assertObjects = Assert(originalResult, mutatedResult);
            bool assertExceptions = Assert(mutatedException, originalException);
            return assertObjects && assertExceptions;
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
