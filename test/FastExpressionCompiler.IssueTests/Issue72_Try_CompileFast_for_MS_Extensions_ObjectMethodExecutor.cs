﻿using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;


namespace FastExpressionCompiler.IssueTests
{

    public class Issue72_Try_CompileFast_for_MS_Extensions_ObjectMethodExecutor : ITest
    {
        public int Run()
        {
            ObjectToStructConversionAndBackShouldWork();
            FastCompiledOK();
            AsyncExecutor_CompiledFast_ExecuteAsync_WithAwait().GetAwaiter().GetResult();
            AsyncExecutor_CompiledFast_ExecuteAsync_WithoutAwait();
            return 4;
        }

        public async Task Foo(int a, int b) => await Task.Delay(1);
        public async Task<int> FooInt(int a, int b) => await Task.FromResult(a + b);
        public async Task<string> FooString(int a, int b) => await Task.FromResult((a + b).ToString());

        //private const string TestMethodName = nameof(Foo);
        private const string TestMethodName = nameof(FooInt);
        //private const string TestMethodName = nameof(FooString);

        private static readonly Type _t = typeof(Issue72_Try_CompileFast_for_MS_Extensions_ObjectMethodExecutor);
        private static readonly TypeInfo _ti = _t.GetTypeInfo();

        private static readonly ObjectMethodExecutor _execCompiled =
            ObjectMethodExecutor.Create(_t.GetMethod(TestMethodName), _ti);

        private static readonly ObjectMethodExecutorCompiledFast _execCompiledFast =
            ObjectMethodExecutorCompiledFast.Create(_t.GetMethod(TestMethodName), _ti);

        private static readonly object[] _parameters = { 1, 2 };


        public void ObjectToStructConversionAndBackShouldWork()
        {
            //(object awaiter) => (object)((TaskAwaiter<int>)awaiter).GetResult();

            var awaiterParamExpr = Expression.Parameter(typeof(object), "awaiter");

            var expr = Expression.Lambda<Func<object, object>>(
                Expression.Convert(
                    Expression.Call(
                        Expression.Convert(awaiterParamExpr, typeof(TaskAwaiter<int>)),
                        typeof(TaskAwaiter<int>).GetTypeInfo()
                            .GetDeclaredMethod(nameof(TaskAwaiter<int>.GetResult))),
                    typeof(object)),
                awaiterParamExpr);

            var result = expr.CompileFast();
            Asserts.IsNotNull(result);

            var awaiter = FooInt(1, 2).GetAwaiter();
            var sum = (int)result(awaiter);
            Asserts.AreEqual(3, sum);
        }


        public void FastCompiledOK()
        {
            var executor = ObjectMethodExecutorCompiledFast.Create(_t.GetMethod(TestMethodName), _ti);
            Asserts.IsNotNull(executor);
            Asserts.IsTrue(executor.IsMethodAsync);

            var sumTask = (Task<int>)executor.Execute(this, new object[] { 1, 2 });
            Asserts.AreEqual(3, sumTask.Result);

            var sum = executor.ExecuteAsync(this, new object[] { 1, 2 });
            Asserts.AreEqual(3, sum.GetAwaiter().GetResult());
        }


        public async Task AsyncExecutor_CompiledNormally_ExecuteAsync_WithAwait()
        {
            await _execCompiled.ExecuteAsync(this, _parameters);
        }


        public async Task AsyncExecutor_CompiledFast_ExecuteAsync_WithAwait()
        {
            await _execCompiledFast.ExecuteAsync(this, _parameters);
        }


        public void AsyncExecutor_CompiledFast_ExecuteAsync_WithoutAwait()
        {
            _execCompiledFast.ExecuteAsync(this, _parameters);
        }
    }
}
