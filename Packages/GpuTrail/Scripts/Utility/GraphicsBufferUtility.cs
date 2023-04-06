using Unity.Collections;
using UnityEngine;

namespace GpuTrailSystem
{
    public static class GraphicsBufferUtility
    {
        public static void Fill<T>(this GraphicsBuffer buffer, T element) where T : struct
        {
            using var array = new NativeArray<T>(buffer.count, Allocator.Temp);
            array.AsSpan().Fill(element);
            
            buffer.SetData(array);
        }
    }
}