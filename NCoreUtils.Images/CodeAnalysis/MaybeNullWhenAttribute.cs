#region Assembly System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// System.Runtime.dll
#endregion


namespace System.Diagnostics.CodeAnalysis
{
    //
    // Summary:
    //     Specifies that when a method returns System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute.ReturnValue,
    //     the parameter may be null even if the corresponding type disallows it.
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class MaybeNullWhenAttribute : Attribute
    {
        //
        // Summary:
        //     Initializes the attribute with the specified return value condition.
        //
        // Parameters:
        //   returnValue:
        //     The return value condition. If the method returns this value, the associated
        //     parameter may be null.
        public MaybeNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

        //
        // Summary:
        //     Gets the return value condition.
        //
        // Returns:
        //     The return value condition. If the method returns this value, the associated
        //     parameter may be null.
        public bool ReturnValue { get; }
    }
}