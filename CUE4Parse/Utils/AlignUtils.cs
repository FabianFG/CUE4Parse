namespace CUE4Parse.Utils
{
    public static class AlignUtils
    {
        public static long Align(this long ptr, int alignment)
        {
            return ((ptr + alignment - 1) & ~(alignment - 1));
        }
        
        public static long Align(this int ptr, int alignment)
        {
            return ((ptr + alignment - 1) & ~(alignment - 1));
        }
    }
}