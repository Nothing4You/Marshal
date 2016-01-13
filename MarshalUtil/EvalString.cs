using System.CodeDom.Compiler;
using System.Reflection;
using Microsoft.CSharp;

namespace MarshalUtil
{
    internal static class EvalString
    {
        /// <summary>
        /// Evaluates string to unescape escaped values by creating a temporary class and executing it<para/>
        /// Use with care!
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Eval(this string str)
        {
            return ParseString(str);
        }

        // Based on http://stackoverflow.com/a/3298747
        private static string ParseString(string txt)
        {
            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerParameters prms = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true
            };
            CompilerResults results = provider.CompileAssemblyFromSource(prms, @"
namespace tmp
{
    public class tmpClass
    {
        public static string GetValue()
        {
             return " + "\"" + txt + "\"" + @";
        }
    }
}");
            Assembly ass = results.CompiledAssembly;
            MethodInfo method = ass.GetType("tmp.tmpClass").GetMethod("GetValue");
            return method.Invoke(null, null) as string;
        }
    }
}
