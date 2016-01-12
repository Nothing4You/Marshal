using System.CodeDom.Compiler;
using System.Reflection;
using Microsoft.CSharp;

namespace MarshalUtil
{
    internal static class EvalString
    {
        // Based on http://stackoverflow.com/a/3298747
        public static string ParseString(string txt)
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
