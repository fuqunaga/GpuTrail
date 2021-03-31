using UnityEngine;


namespace GpuTrailSystem
{

    public static class ComputeShaderUtility
    {
        public static void Dispatch(ComputeShader cs, int kernel, int numThread)
        {
            cs.GetKernelThreadGroupSizes(kernel, out var x, out var _, out var _);

            cs.Dispatch(kernel, Mathf.CeilToInt((float)numThread / x), 1, 1);
        }

    }
}