using Code.Generation.Roslyn;
using System;
using System.Diagnostics;

namespace Alberto.FastMap.CodeGen
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    [CodeGenerationAttribute(typeof(ImplementCloneableInterfaceGenerator))]
    [Conditional("CodeGeneration")]
    public class ImplementCloneableInterfaceAttribute : Attribute
    {
    }
}
