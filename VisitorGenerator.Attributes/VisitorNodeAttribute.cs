using System;

namespace VisitorGenerator
{
    [AttributeUsage(AttributeTargets.Interface)]
    [System.Diagnostics.Conditional("VisitorSourceGenerator_DEBUG")]
    public sealed class VisitorNodeAttribute : Attribute
    {}
}