using Unity.Collections;
using UnityEngine;

namespace GpuTrailSystem
{
    public static class GraphicsBufferUtility
    {
        public static void Fill<T>(this GraphicsBuffer buffer, T element) where T : struct
        {
#if UNITY_2022_2_OR_NEWER
            using var array = new NativeArray<T>(buffer.count, Allocator.Temp);
            array.AsSpan().Fill(element);
            buffer.SetData(array);
#else
            var array = new NativeArray<T>(buffer.count, Allocator.Temp);
            for(var i=0; i<array.Length; i++)
            {
                array[i] = element;
            }
            
            buffer.SetData(array);
            
            array.Dispose();
#endif
        }
    }
}