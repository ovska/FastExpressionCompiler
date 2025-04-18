
using System;
#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{

    public class Issue308_Wrong_delegate_type_returned_with_closure : ITest
    {
        public int Run()
        {
            Test1();
            return 1;
        }

        delegate string Command();


        public void Test1()
        {
            var p = Parameter(typeof(string), "vm");
            var expr = Lambda<Func<string, Command>>(Lambda<Command>(p), p);

            expr.PrintCSharp();

            // var x = (Func<string, Issue308_Wrong_delegate_type_returned_with_closure.Command>)((string vm) => //$
            //     (Issue308_Wrong_delegate_type_returned_with_closure.Command)(() => //$
            //         vm));

            var fSys = expr.CompileSys();
            fSys.PrintIL();

            Asserts.IsInstanceOf<Command>(fSys(null));

            var fFast = expr.CompileFast(true);
            fFast.PrintIL();

            var cmd = fFast("x");
            Asserts.IsInstanceOf<Command>(cmd);
            Asserts.AreEqual("x", cmd());
        }
    }
}