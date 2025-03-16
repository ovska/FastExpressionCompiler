using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
using FastExpressionCompiler.LightExpression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
using System.Linq.Expressions;
namespace FastExpressionCompiler.IssueTests;
#endif

public class Issue451_Operator_implicit_explicit_produces_InvalidProgram : ITest
{
    public int Run()
    {
        Convert_int_to_byte_enum();
        Convert_byte_to_enum();
        Convert_byte_to_nullable_enum();

        Convert_nullable_to_nullable_given_the_conv_op_of_underlying_to_underlying();

        Convert_nullable_enum_into_the_underlying_nullable_type();
        Convert_nullable_enum_into_the_compatible_to_underlying_nullable_type();

        Convert_nullable_enum_using_the_conv_op();
        Convert_nullable_enum_using_the_Passed_conv_method();

        Convert_nullable_enum_using_the_conv_op_with_nullable_param();

        Original_case();
        The_operator_method_is_provided_in_Convert();

        return 10;
    }


#if FALSE // todo: @wip #453 draft of the implementation
    public record struct TestFailure(string Message, string TestName, int SourceLineNumber, string TestsName, string TestsFile);
    public sealed class TestContext
    {
        public uint EvaluatedTestCount;
        public uint FailedTestCount;
        public List<TestFailure> Failures = new();

        public string CurrentTestsName;
        public string CurrentTestsFile;

        internal void AssertFails(string message,
            [CallerMemberName] string testName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            ++EvaluatedTestCount;
            ++FailedTestCount;
            Failures.Add(new TestFailure(message, testName, sourceLineNumber, CurrentTestsName, CurrentTestsFile));
        }

        internal void Register(string testsName, string sourceFilePath)
        {
            CurrentTestsName = testsName;
            CurrentTestsFile = sourceFilePath;
        }
    }

    public struct TestMethodContext
    {
        public readonly TestContext TestContext;
        public TestMethodContext(TestContext context) => TestContext = context;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator TestMethodContext(TestContext t)
        {
            // trick to automatically increment the test count when passing context to the test method
            t.EvaluatedTestCount += 1;
            return new TestMethodContext(t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fails(string message)
        {
            TestContext.FailedTestCount += 1;
        }
    }

    public void Run(TestContext t,
        [CallerFilePath] string sourceFilePath = "<unknown>")
    {
        t.Register(GetType().Name, sourceFilePath);
        TestFoo(t);
        // TestBar(c);
    }

    public void TestFoo(TestMethodContext t)
    {
        t.Fails("Not implemented");
    }
#endif

    public void Convert_byte_to_nullable_enum()
    {
        var conversion = Convert(Constant((byte)5, typeof(byte)), typeof(Hey?));
        var e = Lambda<Func<Hey?>>(conversion);
        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();
        Asserts.AreEqual(Hey.Sailor, fs());

        var ff = e.CompileFast(false);
        ff.PrintIL();
        Asserts.AreEqual(Hey.Sailor, ff());
    }

    public void Convert_byte_to_enum()
    {
        var conversion = Convert(Constant((byte)5, typeof(byte)), typeof(Hey));
        var e = Lambda<Func<Hey>>(conversion);
        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();
        Asserts.AreEqual(Hey.Sailor, fs());

        var ff = e.CompileFast(false);
        ff.PrintIL();
        Asserts.AreEqual(Hey.Sailor, ff());
    }

    public void Convert_int_to_byte_enum()
    {
        var n = Parameter(typeof(int), "n");
        var conversion = Convert(n, typeof(Hey));
        var e = Lambda<Func<int, Hey>>(conversion, n);
        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();
        Asserts.AreEqual((Hey)Byte.MaxValue, fs(Byte.MaxValue));
        Asserts.AreEqual(default(Hey), fs(Byte.MaxValue + 1));

        var ff = e.CompileFast(false);
        ff.PrintIL();
        Asserts.AreEqual((Hey)Byte.MaxValue, ff(Byte.MaxValue));
        Asserts.AreEqual(default(Hey), ff(Byte.MaxValue + 1));
    }

    public struct SampleType
    {
        public bool? Value { get; set; }

        public SampleType(bool? value) { Value = value; }

        public static implicit operator bool?(SampleType left) =>
            (left.Value is not null && left.Value is bool b) ? b : null;

        public static explicit operator bool?(SampleType? left) =>
            left == null ? null : (left.Value.Value is bool b && b) ? b : null;

        public static explicit operator bool(SampleType left) =>
            left.Value is not null && left.Value is bool b && b;
    }

    public enum Hey : byte { Sailor = 5 }

    public void Convert_nullable_enum_into_the_underlying_nullable_type()
    {
        var conversion = Convert(Constant(Hey.Sailor, typeof(Hey?)), typeof(byte?));
        var e = Lambda<Func<byte?>>(conversion);
        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();
        Asserts.AreEqual(5, fs().Value);

        var ff = e.CompileFast(false);
        ff.PrintIL();
        Asserts.AreEqual(5, ff().Value);
    }

    public void Convert_nullable_enum_into_the_compatible_to_underlying_nullable_type()
    {
        var conversion = Convert(Constant(Hey.Sailor, typeof(Hey?)), typeof(int?));
        var e = Lambda<Func<int?>>(conversion);
        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();
        Asserts.AreEqual(5, fs());

        var ff = e.CompileFast(false);
        ff.PrintIL();
        Asserts.AreEqual(5, ff());
    }

    public class Foo
    {
        public int Value;
        public static explicit operator Foo(byte b) => new Foo { Value = b }; // unused
        public static explicit operator Foo(Hey hey) => new Foo { Value = (int)hey };
    }

    public void Convert_nullable_enum_using_the_conv_op()
    {
        var conversion = Convert(Constant(Hey.Sailor, typeof(Hey?)), typeof(Foo));
        var e = Lambda<Func<Foo>>(conversion);
        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();
        Asserts.AreEqual(5, fs().Value);

        var ff = e.CompileFast(false);
        ff.PrintIL();
        Asserts.AreEqual(5, ff().Value);
    }

    public void Convert_nullable_enum_using_the_Passed_conv_method()
    {
        var conversion = Convert(Constant(Hey.Sailor, typeof(Hey?)), typeof(Foo),
            typeof(Foo).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "op_Explicit" &&
                    m.GetParameters()[0].ParameterType == typeof(Hey)));

        var e = Lambda<Func<Foo>>(conversion);
        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();
        Asserts.AreEqual(5, fs().Value);

        var ff = e.CompileFast(false);
        ff.PrintIL();
        Asserts.AreEqual(5, ff().Value);
    }

    public struct Bar
    {
        public int? Value;
        public static explicit operator Bar(int? n) => new Bar { Value = n };
        public static explicit operator Bar(Hey? hey) => new Bar { Value = hey.HasValue ? (int)hey.Value : null };
        public static explicit operator Bar?(Hey? hey) => !hey.HasValue ? null : new Bar { Value = (int)hey.Value };
    }

    public void Convert_nullable_enum_using_the_conv_op_with_nullable_param()
    {
        var conversion = Convert(Constant(null, typeof(Hey?)), typeof(Bar));
        var e = Lambda<Func<Bar>>(conversion);
        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();
        Asserts.IsNull(fs().Value);

        var ff = e.CompileFast(false);
        ff.PrintIL();
        Asserts.IsNull(ff().Value);
    }

    public struct Jazz
    {
        public int Value;
        public static explicit operator Jazz(int n) => new Jazz { Value = n };
    }

    public void Convert_nullable_to_nullable_given_the_conv_op_of_underlying_to_underlying()
    {
        var conversion = Convert(Constant(42, typeof(int?)), typeof(Jazz?));
        var e = Lambda<Func<Jazz?>>(conversion);
        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();
        Asserts.AreEqual(42, fs().Value.Value);

        var ff = e.CompileFast(false);
        ff.PrintIL();
        Asserts.AreEqual(42, fs().Value.Value);
    }

    public void Original_case()
    {
        var ctorMethodInfo = typeof(SampleType).GetConstructors()[0];

        var newExpression = New(ctorMethodInfo, Constant(null, typeof(bool?)));

        var conversion1 = Convert(newExpression, typeof(bool));
        var lambda1 = Lambda<Func<bool>>(conversion1);
        lambda1.PrintCSharp();

        var conversion2 = Convert(newExpression, typeof(bool?));
        var lambda2 = Lambda<Func<bool?>>(conversion2);
        lambda2.PrintCSharp();

        var sample1 = lambda1.CompileSys();
        sample1.PrintIL();
        Asserts.AreEqual(false, sample1());

        var sample2 = lambda2.CompileSys();
        sample2.PrintIL();
        Asserts.IsNull(sample2());

        // <- OK
        var sample_fast1 = lambda1.CompileFast(false);
        sample_fast1.PrintIL();
        Asserts.AreEqual(false, sample_fast1());

        // <- throws exception
        var sample_fast2 = lambda2.CompileFast(false);
        sample_fast2.PrintIL();
        Asserts.IsNull(sample_fast2());
    }

    public void The_operator_method_is_provided_in_Convert()
    {
        var ctorMethodInfo = typeof(SampleType).GetConstructors()[0];

        var newExpression = New(ctorMethodInfo, Constant(null, typeof(bool?)));

        // let's use the explicit operator method which converts to bool
        var convertToNullableBoolMethod = typeof(SampleType).FindConvertOperator(typeof(SampleType), typeof(bool));
        var conversion = Convert(newExpression, typeof(bool?), convertToNullableBoolMethod);

        var lambda = Lambda<Func<bool?>>(conversion);
        lambda.PrintCSharp();

        var sample = lambda.CompileSys();
        sample.PrintIL();
        Asserts.AreEqual(false, sample());

        var sample_fast = lambda.CompileFast(false);
        sample_fast.PrintIL();
        Asserts.AreEqual(false, sample_fast());
    }
}