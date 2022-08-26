﻿#if !LIGHT_EXPRESSION
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Buffers.Binary;
using NUnit.Framework;
namespace FastExpressionCompiler.IssueTests
{
    [TestFixture]
    public class EmitHacksTest : ITest
    {
        public int Run()
        {
            DynamicMethod_Emit_Hack();
            // DynamicMethod_Emit_Newobj();
            // DynamicMethod_Hack_Emit_Newobj();
            return 2;
        }

        [Test]
        public void DynamicMethod_Emit_Hack()
        {
            var f = Get_DynamicMethod_Emit_Hack();
            var a = f();
            Assert.AreEqual(42, a);
        }

        public static Func<int> Get_DynamicMethod_Emit_Hack()
        {
            var dynMethod = new DynamicMethod(string.Empty,
                typeof(int), new[] { typeof(ExpressionCompiler.ArrayClosure) },
                typeof(ExpressionCompiler), skipVisibility: true);

            var il = dynMethod.GetILGenerator();
            var ilType = il.GetType();
            var opCode = OpCodes.Call;
            var meth = MethodStaticNoArgs;
            var paramCount = 0;

            // // stripped down code for not generic method and not array method:
            // public override void Emit(OpCode opcode, MethodInfo meth, int paramCount)
            // {

            //     m_scope.m_tokens.Add(((RuntimeMethodInfo)meth).MethodHandle);
            var methodHandle = meth.MethodHandle;
            var mScopeField = ilType.GetField("m_scope", BindingFlags.Instance | BindingFlags.NonPublic);
            if (mScopeField == null)
                return null;
            var mScope = mScopeField.GetValue(il);

            var mTokensField = mScope.GetType().GetField("m_tokens", BindingFlags.Instance | BindingFlags.NonPublic);
            if (mTokensField == null)
                return null;

            var mTokens = (IList<object?>)mTokensField.GetValue(mScope);
            mTokens.Add(methodHandle);

            //     var token = m_scope.m_tokens.Count - 1 | (int)MetadataTokenType.MethodDef; // MethodDef is 0x06000000
            var token = mTokens.Count - 1 | (int)0x06000000; // MetadataTokenType.MethodDef
            Console.WriteLine("token: {0}", token);

            //     // Guarantees an array capable of holding at least size elements.
            //     if (m_length + 7 >= m_ILStream.Length)
            //         IncreaseCapacity(7);
            var mLengthField = typeof(ILGenerator).GetField("m_length", BindingFlags.Instance | BindingFlags.NonPublic);
            if (mLengthField == null)
                return null;
            var mLength = (int)mLengthField.GetValue(il);

            var mILStreamField = typeof(ILGenerator).GetField("m_ILStream", BindingFlags.Instance | BindingFlags.NonPublic);
            if (mILStreamField == null)
                return null;
            var mILStream = (byte[])mILStreamField.GetValue(il);
            if (mILStream.Length < mLength + 7)
                Array.Resize(ref mILStream, Math.Max(mILStream.Length * 2, mLength + 7));

            //     m_ILStream[m_length++] = (byte)opcode.Value;
            mILStream[mLength++] = (byte)opCode.Value;
            mLengthField.SetValue(il, mLength);

            //     UpdateStackSize(opcode, 0);
            var updateStackSize = ilType.GetMethod("UpdateStackSize", BindingFlags.Instance | BindingFlags.NonPublic);
            if (updateStackSize == null)
                return null;
            updateStackSize.Invoke(il, new object[] { opCode, 0 });

            int stackchange = 0;
            if (meth.ReturnType != typeof(void))
                stackchange++;
            stackchange -= paramCount;
            if (!meth.IsStatic)
                stackchange--;

            //     UpdateStackSize(opcode, stackchange);
            updateStackSize.Invoke(il, new object[] { opCode, stackchange });

            BinaryPrimitives.WriteInt32LittleEndian(mILStream.AsSpan(mLength), token);
            //     m_length += 4;
            mLengthField.SetValue(il, mLength + 4);
            // }


            il.Emit(OpCodes.Ret);

            return (Func<int>)dynMethod.CreateDelegate(typeof(Func<int>), ExpressionCompiler.EmptyArrayClosure);
        }

        [Test]
        public void DynamicMethod_Emit_OpCodes_Call()
        {
            var f = Get_DynamicMethod_Emit_OpCodes_Call();
            var a = f();
            Assert.AreEqual(42, a);
        }

        public static Func<int> Get_DynamicMethod_Emit_OpCodes_Call()
        {
            var dynMethod = new DynamicMethod(string.Empty,
                typeof(int), new[] { typeof(ExpressionCompiler.ArrayClosure) },
                typeof(ExpressionCompiler), skipVisibility: true);

            var il = dynMethod.GetILGenerator();

            il.Emit(OpCodes.Call, MethodStaticNoArgs);
            il.Emit(OpCodes.Ret);

            return (Func<int>)dynMethod.CreateDelegate(typeof(Func<int>), ExpressionCompiler.EmptyArrayClosure);
        }

        [Test]
        public void DynamicMethod_Emit_Newobj()
        {
            var f = Get_DynamicMethod_Emit_Newobj();
            var a = f();
            Assert.IsInstanceOf<A>(a);
        }

        // [Test]
        public void DynamicMethod_Hack_Emit_Newobj()
        {
            var f = Get_DynamicMethod_Hack_Emit_Newobj();
            var a = f();
            Assert.IsInstanceOf<A>(a);
        }

        public static Func<A> Get_DynamicMethod_Emit_Newobj()
        {
            var dynMethod = new DynamicMethod(string.Empty,
                typeof(A), new[] { typeof(ExpressionCompiler.ArrayClosure) },
                typeof(ExpressionCompiler), skipVisibility: true);

            var il = dynMethod.GetILGenerator();

            il.Emit(OpCodes.Newobj, _ctor);
            il.Emit(OpCodes.Ret);

            return (Func<A>)dynMethod.CreateDelegate(typeof(Func<A>), ExpressionCompiler.EmptyArrayClosure);
        }

        public static Func<A> Get_DynamicMethod_Hack_Emit_Newobj()
        {
            var dynMethod = new DynamicMethod(string.Empty,
                typeof(A), new[] { typeof(ExpressionCompiler.ArrayClosure) },
                typeof(ExpressionCompiler), skipVisibility: true);

            var il = dynMethod.GetILGenerator();
            var ilType = il.GetType();

            il.Emit(OpCodes.Newobj, _ctor);

            Debug.Assert(_ctor.DeclaringType != null && !_ctor.DeclaringType.IsGenericType);
            // var rtConstructor = con as RuntimeConstructorInfo;
            var methodHandle = _ctor.MethodHandle;

            // m_tokens.Add(rtConstructor.MethodHandle);
            // var tk = m_tokens.Count - 1 | (int)MetadataTokenType.MethodDef;

            var mScopeField = ilType.GetField("m_scope", BindingFlags.Instance | BindingFlags.NonPublic);
            if (mScopeField == null)
                return null;
            var mScope = mScopeField.GetValue(il);

            var mTokensField = mScope.GetType().GetField("m_tokens", BindingFlags.Instance | BindingFlags.NonPublic);
            if (mTokensField == null)
                return null;
            var mTokens = mTokensField.GetValue(mScope);


            il.Emit(OpCodes.Ret);

            return (Func<A>)dynMethod.CreateDelegate(typeof(Func<A>), ExpressionCompiler.EmptyArrayClosure);
        }

        public class A
        {
            public A() { }

            public static int M() => 42;
        }

        private static readonly ConstructorInfo _ctor = typeof(A).GetConstructor(Type.EmptyTypes);
        public static readonly MethodInfo MethodStaticNoArgs = typeof(A).GetMethod(nameof(A.M));
    }
}
#endif