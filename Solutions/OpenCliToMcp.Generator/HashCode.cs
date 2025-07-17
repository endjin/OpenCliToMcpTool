#if NETSTANDARD2_0

using System;

namespace OpenCliToMcp.Generator
{
    /// <summary>
    /// Simplified HashCode implementation for .NET Standard 2.0
    /// </summary>
    internal static class HashCode
    {
        public static int Combine<T1>(T1 value1)
        {
            return value1?.GetHashCode() ?? 0;
        }
        
        public static int Combine<T1, T2>(T1 value1, T2 value2)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (value1?.GetHashCode() ?? 0);
                hash = hash * 31 + (value2?.GetHashCode() ?? 0);
                return hash;
            }
        }
        
        public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (value1?.GetHashCode() ?? 0);
                hash = hash * 31 + (value2?.GetHashCode() ?? 0);
                hash = hash * 31 + (value3?.GetHashCode() ?? 0);
                return hash;
            }
        }
        
        public static int Combine<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (value1?.GetHashCode() ?? 0);
                hash = hash * 31 + (value2?.GetHashCode() ?? 0);
                hash = hash * 31 + (value3?.GetHashCode() ?? 0);
                hash = hash * 31 + (value4?.GetHashCode() ?? 0);
                return hash;
            }
        }
        
        public static int Combine<T1, T2, T3, T4, T5>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (value1?.GetHashCode() ?? 0);
                hash = hash * 31 + (value2?.GetHashCode() ?? 0);
                hash = hash * 31 + (value3?.GetHashCode() ?? 0);
                hash = hash * 31 + (value4?.GetHashCode() ?? 0);
                hash = hash * 31 + (value5?.GetHashCode() ?? 0);
                return hash;
            }
        }
        
        public static int Combine<T1, T2, T3, T4, T5, T6>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (value1?.GetHashCode() ?? 0);
                hash = hash * 31 + (value2?.GetHashCode() ?? 0);
                hash = hash * 31 + (value3?.GetHashCode() ?? 0);
                hash = hash * 31 + (value4?.GetHashCode() ?? 0);
                hash = hash * 31 + (value5?.GetHashCode() ?? 0);
                hash = hash * 31 + (value6?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}

#endif