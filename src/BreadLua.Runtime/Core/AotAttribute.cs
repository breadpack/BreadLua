#if !UNITY_5_3_OR_NEWER
namespace AOT
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    internal class MonoPInvokeCallbackAttribute : System.Attribute
    {
        public MonoPInvokeCallbackAttribute(System.Type type) { }
    }
}
#endif
