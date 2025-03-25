﻿#if DEBUG
#define PRINTIL
#define PRINTCS
#endif
using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using FastExpressionCompiler.ILDecoder;
using System.IO;
using System.Collections.Generic;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression;
using FastExpressionCompiler.LightExpression.ImTools;
#else
namespace FastExpressionCompiler;
using FastExpressionCompiler.ImTools;
using System.Linq.Expressions;
#endif

public static class TestTools
{
    public static void AssertOpCodes(this Delegate @delegate, params OpCode[] expectedCodes) =>
        AssertOpCodes(@delegate.Method, expectedCodes);

    public static void AssertOpCodes(this MethodInfo method, params OpCode[] expectedCodes)
    {
        var ilReader = ILReaderFactory.Create(method);
        if (ilReader is null)
        {
            Debug.WriteLine($"Reading IL is currently not supported");
            return;
        }
        var actualCodes = ilReader.Select(x => x.OpCode).ToArray();

        var sb = new StringBuilder();
        var index = 0;
        foreach (var code in actualCodes)
        {
            if (index < 1000)
                sb.AppendLine($"{index,-4}{code}");
            else if (index < 10000000)
                sb.AppendLine($"{index,-8}{code}");
            else
                sb.AppendLine($"{index,-12}{code}");
            ++index;
        }

        Asserts.AreEqual(expectedCodes, actualCodes);
    }

    [Conditional("DEBUG")]
    public static void PrintExpression(this Expression expr, bool completeTypeNames = false) =>
        Console.WriteLine(
            expr.ToExpressionString(out var _, out var _, out var _,
            stripNamespace: true,
            printType: completeTypeNames ? null : CodePrinter.PrintTypeStripOuterClasses,
            indentSpaces: 4)
        );

    [Conditional("DEBUG")]
    public static void PrintCSharp(this Expression expr, bool completeTypeNames = false,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "")
    {
#if PRINTCS
        Console.WriteLine();
        Console.WriteLine($"//{Path.GetFileNameWithoutExtension(filePath)}.{caller}");

        var sb = new StringBuilder(1024);
        sb.Append("var @cs = ");
        sb = expr.ToCSharpString(sb,
            lineIndent: 0,
            stripNamespace: true,
            printType: completeTypeNames ? null : CodePrinter.PrintTypeStripOuterClasses,
            indentSpaces: 4);
        sb.Append(';');
        Console.WriteLine(sb.ToString());
#endif
    }

    [Conditional("DEBUG")]
    public static void PrintCSharp(this Expression expr, Func<string, string> transform,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "")
    {
#if PRINTCS
        Console.WriteLine();
        Console.WriteLine($"//{Path.GetFileNameWithoutExtension(filePath)}.{caller}");
        Console.WriteLine(transform(expr.ToCSharpString()));
#endif
    }

    [Conditional("DEBUG")]
    public static void PrintCSharp(this Expression expr, CodePrinter.ObjectToCode objectToCode,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "")
    {
#if PRINTCS
        Console.WriteLine();
        Console.WriteLine($"//{Path.GetFileNameWithoutExtension(filePath)}.{caller}");
        Console.WriteLine(expr.ToCSharpString(objectToCode));
#endif
    }

    [Conditional("DEBUG")]
    public static void PrintCSharp(this Expression expr, ref string result)
    {
#if PRINTCS
        Console.WriteLine(result = expr.ToCSharpString());
#endif
    }

    [Conditional("DEBUG")]
    public static void PrintIL(this Delegate @delegate, [CallerMemberName] string tag = null)
    {
#if PRINTIL
        @delegate.Method.PrintIL(tag);
#endif
    }

    [Conditional("DEBUG")]
    public static void PrintIL(this MethodInfo method, string tag = null)
    {
#if PRINTIL
        var s = new StringBuilder();
        s.Append(tag == null ? "<il>" : "<" + tag + ">").AppendLine();
        method.ToILString(s);
        s.AppendLine().Append(tag == null ? "</il>" : "</" + tag + ">");
        Console.WriteLine(s);
#endif
    }
}

// todo: @wip @feat #453 replacing the last NUnit bone of Assert
public static class Asserts
{
    public sealed class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AreSame<T>(T expected, T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
        string expectedName = "expected",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") where T : class =>
        ReferenceEquals(expected, actual) ? true : throw new AssertionException(
            $"Expected `AreSame({expectedName}, {actualName})`, but found `{expected.ToCode()}` is Not the same `{actual.ToCode()}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AreNotSame<T>(T expected, T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
        string expectedName = "expected",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") where T : class =>
        !ReferenceEquals(expected, actual) ? true : throw new AssertionException(
            $"Expected `AreNotSame({expectedName}, {actualName})`, but found `{expected.ToCode()}` is same as `{actual.ToCode()}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AreEqual<T>(T expected, T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
        string expectedName = "expected",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") =>
        Equals(expected, actual) ? true : throw new AssertionException(
            $"Expected `AreEqual({expectedName}, {actualName})`, but found `{expected.ToCode()}` is Not equal to `{actual.ToCode()}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AreNotEqual<T>(T expected, T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
        string expectedName = "expected",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") =>
        !Equals(expected, actual) ? true : throw new AssertionException(
            $"Expected `AreNotEqual({expectedName}, {actualName})`, but found `{expected.ToCode()}` is equal to `{actual.ToCode()}`");

    public record struct ItemsCompared<T>(int Index, bool IsEqual, T Expected, T Actual);

    /// <summary>Should cover the case with the `expected` to be an array as well.</summary>
    public static bool AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
        string expectedName = "expected",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual")
    {
        var expectedEnumerator = expected.GetEnumerator();
        var actualEnumerator = actual.GetEnumerator();
        var expectedCount = 0;
        var actualCount = 0;

        // Collecting the context around the non-equal items, lets call it a window,
        // If the window size is 8 it means: 
        // - it will be 4 or less equal items before the first non-equal,
        // - 8 or less equal items between the non-equal items (if there more than 8 equal items inbetween, then it will be 4 after and 4 before the next non-equal item)
        // - 4 or less equal items after the last non-equal item

        // Example output with window size 2:
        // #0  == 1, 1
        // #1  == 3, 3
        // #2  != 3, 4
        // #3  == 7, 7
        // #4  == 9, 9
        // ...
        // #10 == 51, 51
        // #11 == 53, 53
        // #12 != 55, 53
        //
        const int ContextWindowCount = 8;
        const int HalfContextWindowCount = ContextWindowCount >> 1;
        const int MaskHalfContextWindowCount = HalfContextWindowCount - 1;
        const int MaxNonEqualItemCount = 64;

        // The equal items in a window from the start of collections of after the previous non-equal item and until the next non-equal item
        var beforeNonEqualCount = 0;

        // Counter track the number the equal items collected after non-equal, should be between 0 and HalfContextWindowCount, starts from HalfContextWindowCount
        var afterNonEqualReverseCount = 0;

        // Using those 4 slots directly to represent recent 4 equal items, before the non-equal item.
        // The slots will be rotated by overriding the `a` again, when the `d` is reached, then the `b`, etc.
        ItemsCompared<T> a = default, b = default, c = default, d = default;
        SmallList4<ItemsCompared<T>> collectedItems = default;

        var nonEqualItemCount = 0;
        var collectedMaxNonEqualItems = false;

        // Traverse until the end of the largest collection
        var hasExpected = true;
        var hasActual = true;
        for (var index = 0; hasExpected | hasActual; ++index)
        {
            hasExpected = hasExpected && expectedEnumerator.MoveNext();
            if (hasExpected) ++expectedCount;

            hasActual = hasActual && actualEnumerator.MoveNext();
            if (hasActual) ++actualCount;

            // todo: @wip if the one collection is completed, it still be good to add the item to context from the other collection
            if (!collectedMaxNonEqualItems & hasExpected & hasActual)
            {
                var exp = expectedEnumerator.Current;
                var act = actualEnumerator.Current;
                if (!Equals(exp, act))
                {
                    // It's done after we found one more non-equal item, faster than collecting the last non-equal context
                    collectedMaxNonEqualItems = nonEqualItemCount > MaxNonEqualItemCount;
                    if (!collectedMaxNonEqualItems)
                    {
                        // Add the collected context items before the non-equal item to the whole list of items
                        if (beforeNonEqualCount != 0)
                        {
                            if (beforeNonEqualCount < HalfContextWindowCount)
                            {
                                switch (beforeNonEqualCount)
                                {
                                    case 1: collectedItems.Add(in a); break;
                                    case 2: collectedItems.Add(in a); collectedItems.Add(in b); break;
                                    case 3: collectedItems.Add(in a); collectedItems.Add(in b); collectedItems.Add(in c); break;
                                }
                            }
                            else
                            {
                                switch (beforeNonEqualCount & MaskHalfContextWindowCount)
                                {
                                    case 0: collectedItems.Add(in a); collectedItems.Add(in b); collectedItems.Add(in c); collectedItems.Add(in d); break;
                                    case 1: collectedItems.Add(in b); collectedItems.Add(in c); collectedItems.Add(in d); collectedItems.Add(in a); break;
                                    case 2: collectedItems.Add(in c); collectedItems.Add(in d); collectedItems.Add(in a); collectedItems.Add(in b); break;
                                    case 3: collectedItems.Add(in d); collectedItems.Add(in a); collectedItems.Add(in b); collectedItems.Add(in c); break;
                                }
                            }
                            beforeNonEqualCount = 0; // reset the count of equal items before the non-equal item
                        }

                        ++nonEqualItemCount;
                        collectedItems.Add(new ItemsCompared<T>(index, false, exp, act));
                        afterNonEqualReverseCount = HalfContextWindowCount;
                    }
                }
                else
                {
                    if (afterNonEqualReverseCount > 0)
                    {
                        collectedItems.Add(new ItemsCompared<T>(index, true, exp, act));
                        --afterNonEqualReverseCount;
                        if (afterNonEqualReverseCount == 0) // stop when the full equal context is collected for the last non-equal item
                            collectedMaxNonEqualItems = nonEqualItemCount >= MaxNonEqualItemCount;
                    }
                    else
                    {
                        // Collecting the equal items for the next non-equal context window
                        switch (beforeNonEqualCount++ & MaskHalfContextWindowCount)
                        {
                            case 0: a = new ItemsCompared<T>(index, true, exp, act); break;
                            case 1: b = new ItemsCompared<T>(index, true, exp, act); break;
                            case 2: c = new ItemsCompared<T>(index, true, exp, act); break;
                            case 3: d = new ItemsCompared<T>(index, true, exp, act); break;
                        }
                    }
                }
            }
        }

        if (nonEqualItemCount != 0 | expectedCount != actualCount)
        {
            var sb = new StringBuilder();

            sb.Append($"Expected collections `AreEqual({expectedName}, {actualName})`, but found ");
            if (expectedCount != actualCount)
                sb.Append($"the different counts {expectedCount} != {actualCount}");

            if (nonEqualItemCount != 0)
            {
                if (expectedCount != actualCount)
                    sb.Append(" and ");
                if (nonEqualItemCount < MaxNonEqualItemCount)
                    sb.AppendLine($"{nonEqualItemCount} non equal items:");
                else
                    sb.AppendLine($"first {MaxNonEqualItemCount} non equal items (and stopped searching):");

                foreach (var (index, isEqual, expectedItem, actualItem) in collectedItems.Enumerate())
                    sb.AppendLine($"{index,4}{(isEqual ? "    " : " -> ")}{expectedItem.ToCode(),16},{actualItem.ToCode(),16}");
            }

            throw new AssertionException(sb.ToString());
        }

        return true;
    }

    public static bool AreEqual<T>(T[] expected, T[] actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
        string expectedName = "expected",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") =>
        AreEqual((IEnumerable<T>)expected, actual, expectedName, actualName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterOrEqual<T>(T expected, T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
        string expectedName = "expected",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual")
        where T : IComparable<T> =>
        expected.CompareTo(actual) >= 0 ? true : throw new AssertionException(
            $"Expected `GreaterOrEqual({expectedName}, {actualName})`, but found `{expected.ToCode()} < {actual.ToCode()}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Less<T>(T expected, T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
        string expectedName = "expected",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual")
        where T : IComparable<T> =>
        expected.CompareTo(actual) < 0 ? true : throw new AssertionException(
            $"Expected `Less({expectedName}, {actualName})`, but found `{expected.ToCode()} >= {actual.ToCode()}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Greater<T>(T expected, T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
        string expectedName = "expected",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual")
        where T : IComparable<T> =>
        expected.CompareTo(actual) > 0 ? true : throw new AssertionException(
            $"Expected `Greater({expectedName}, {actualName})`, but found `{expected.ToCode()} <= {actual.ToCode()}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessOrEqual<T>(T expected, T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
        string expectedName = "expected",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual")
        where T : IComparable<T> =>
        expected.CompareTo(actual) <= 0 ? true : throw new AssertionException(
            $"Expected `LessOrEqual({expectedName}, {actualName})`, but found `{expected.ToCode()} > {actual.ToCode()}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNull<T>(T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") where T : class =>
        actual is null ? true : throw new AssertionException(
            $"Expected `IsNull({actualName})`, but found not null `{actual.ToCode()}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNull<T>(T? actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") where T : struct =>
        !actual.HasValue ? true : throw new AssertionException(
            $"Expected the nullable `IsNull({actualName})`, but found it has a value `{actual.Value}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotNull<T>(T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") where T : class =>
        actual is not null ? true : throw new AssertionException(
            $"Expected `IsNotNull({actualName})`, but found null");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotNull<T>(T? actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") where T : struct =>
        actual.HasValue ? true : throw new AssertionException(
            $"Expected the nullable `IsNotNull({actualName})`, but found null");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsTrue(bool actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") =>
        actual ? true : throw new AssertionException(
            $"Expected `IsTrue({actualName})`, but found false");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFalse(bool actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") =>
        !actual ? true : throw new AssertionException(
            $"Expected `IsFalse({actualName})`, but found true");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInstanceOf<T>(object actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") =>
        actual is T ? true : throw new AssertionException(
            $"Expected `IsInstanceOf<{typeof(T).ToCode()}>({actualName})`, but found `IsInstanceOf<{actual?.GetType().ToCode() ?? "_"}>({actual.ToCode()})`");

    public static E Throws<E>(Action action,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(action))]
#endif
        string actionName = "<action to throw>")
        where E : Exception
    {
        try
        {
            action();
        }
        catch (E ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            throw new AssertionException(
                $"Expected `Throws<{typeof(E).ToCode()}>({actionName})`, but found it throws `{ex.GetType().ToCode()}` with message '{ex.Message}'");
        }
        throw new AssertionException($"Expected `Throws<{typeof(E).ToCode()}>({actionName})`, but no exception was thrown");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(string expected, string actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
        string expectedName = "expected",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") =>
        actual.Contains(expected) ? true : throw new AssertionException(
            $"Expected string `Contains({expectedName}, {actualName})`, but found expected `{expected.ToCode()}` is not in `{actual.ToCode()}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DoesNotContain(string expected, string actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
        string expectedName = "expected",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") =>
        !actual.Contains(expected) ? true : throw new AssertionException(
            $"Expected string `DoesNotContain({expectedName}, {actualName})`, but found expected `{expected.ToCode()}` is in `{actual.ToCode()}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StartsWith(string expected, string actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
        string expectedName = "expected",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") =>
        actual.StartsWith(expected) ? true : throw new AssertionException(
            $"Expected string `StartsWith({expectedName}, {actualName})`, but found expected `{expected.ToCode()}` is not at start of `{actual.ToCode()}`");
}

public interface ITest
{
    int Run();
}

public interface ITestX
{
    void Run(TestRun tr);
}

public enum AssertKind
{
    CommandedToFail,
    IsTrue,
    IsFalse,
    AreEqual,
    AreNotEqual,
    Throws,
}

public record struct TestFailure(
    string TestMethodName,
    int SourceLineNumber,
    AssertKind Kind,
    string Message);

public record struct TestStats(
    string TestsName,
    string TestsFile,
    Exception TestStopException,
    int TestCount,
    int FirstFailureIndex,
    int FailureCount);

public enum TestTracking
{
    TrackFailedTestsOnly = 0,
    TrackAllTests,
}

// Wrapper for the context per test method
public struct TestContext
{
    public readonly TestRun TestRun;
    public TestContext(TestRun testRun) => TestRun = testRun;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator TestContext(TestRun t)
    {
        // A trick to automatically increment the test count when passing context to the test method
        t.TotalTestCount += 1;
        return new TestContext(t);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AddFailure(string testName, int sourceLineNumber, AssertKind assertKind, string message)
    {
        TestRun.Failures.Add(new TestFailure(testName, sourceLineNumber, assertKind, message));
        return false;
    }

    /// <summary>Always failes with the provided message</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Fail(string message,
#if NETCOREAPP3_0_OR_GREATER
            [CallerMemberName]
#endif
        string testName = "<test>",
#if NETCOREAPP3_0_OR_GREATER
            [CallerLineNumber]
#endif
        int sourceLineNumber = -1) =>
        AddFailure(testName, sourceLineNumber, AssertKind.CommandedToFail, message);

    /// <summary>Checks if `actual is true`. Method returns `bool` so the latter test logic may depend on it, e.g. to return early</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsTrue(bool actual,
#if NETCOREAPP3_0_OR_GREATER
            [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "<actual>",
#if NETCOREAPP3_0_OR_GREATER
            [CallerMemberName]
#endif
        string testName = "<test>",
#if NETCOREAPP3_0_OR_GREATER
            [CallerLineNumber]
#endif
        int sourceLineNumber = -1) =>
        actual ||
            AddFailure(testName, sourceLineNumber, AssertKind.IsTrue,
                $"Expected `IsTrue({actualName})`, but found false");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AreEqual<T>(T expected, T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
    string expectedName = "<expected>",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
    string actualName = "<actual>",
#if NETCOREAPP3_0_OR_GREATER
            [CallerMemberName]
#endif
        string testName = "<test>",
#if NETCOREAPP3_0_OR_GREATER
            [CallerLineNumber]
#endif
        int sourceLineNumber = -1) =>
    Equals(expected, actual) ||
        AddFailure(testName, sourceLineNumber, AssertKind.AreEqual,
            $"Expected `AreEqual({expectedName}, {actualName})`, but found `{expected.ToCode()}` is Not equal to `{actual.ToCode()}`");
}

/// <summary>Per-thread context, accumulating the stats and failures in its Run method.</summary>
public sealed class TestRun
{
    public int TotalTestCount;
    // todo: @perf it may use ImTools.SmallList for the stats and failures to more local access to the Count
    public List<TestStats> Stats = new();
    public List<TestFailure> Failures = new();

    public void Run(ITestX test, TestTracking tracking = TestTracking.TrackFailedTestsOnly)
    {
        var totalTestCount = TotalTestCount;
        var failureCount = Failures.Count;
        Exception testStopException = null;
        try
        {
            test.Run(this);
        }
        catch (Exception ex)
        {
            testStopException = ex;
        }

        var testFailureCount = Failures.Count - failureCount;
        if (testStopException != null ||
            tracking == TestTracking.TrackAllTests ||
            tracking == TestTracking.TrackFailedTestsOnly & testFailureCount > 0)
        {
            // todo: @wip is there a more performant way to get the test name and file?
            var testsType = test.GetType();
            var testsName = testsType.Name;
            var testsFile = new Uri(testsType.Assembly.Location).LocalPath;

            var testCount = TotalTestCount - totalTestCount;

            var stats = new TestStats(testsName, testsFile, testStopException, testCount, failureCount, testFailureCount);
            Stats.Add(stats);

            // todo: @wip better output?
            // Output the failures:
            if (testStopException != null)
            {
                Console.WriteLine($"Unexpected exception in test '{testsName}':{Environment.NewLine}'{testStopException}'");
            }
            if (testFailureCount > 0)
            {
                Console.WriteLine($"Test '{testsName}' failed {testFailureCount} time{(testFailureCount == 1 ? "" : "s")}:");
                for (var i = 0; i < testFailureCount; ++i)
                {
                    var f = Failures[failureCount + i];
                    Console.WriteLine($"#{i} at line {f.SourceLineNumber}:{Environment.NewLine}'{f.Message}'");
                }
            }
        }
    }
}