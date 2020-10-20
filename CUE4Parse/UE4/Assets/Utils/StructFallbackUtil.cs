using System;
using CUE4Parse.UE4.Assets.Objects;

namespace CUE4Parse.UE4.Assets.Utils
{
    [AttributeUsage(AttributeTargets.Class)]
    public class StructFallback : Attribute {}
    
    public static class StructFallbackUtil
    {
        public static object? MapToClass(this FStructFallback fallback, Type type)
        {
            var value = Activator.CreateInstance(type, fallback);
            //var value = type.GetConstructor(new[] { typeof(FStructFallback) })?.Invoke(new object[] { fallback });
            return value;
        }
    }
}