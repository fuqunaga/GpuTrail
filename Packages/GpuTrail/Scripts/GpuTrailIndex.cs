using UnityEngine;


namespace GpuTrailSystem
{
    /// <summary>
    /// C# side corresponding to GpuTrailIndexInclude.cginc
    /// </summary>
    public class GpuTrailIndex
    {
        #region Static

        public static class CSParam
        {
            public static readonly string Keyword_TrailIdxOn = "GPUTRAIL_TRAIL_INDEX_ON";
            public static readonly int TrailIndexBuffer = Shader.PropertyToID("_TrailIndexBuffer");
        }


        public static void SetComputeShaderParameterEnable(ComputeShader cs, int kernel, GraphicsBuffer trailIndexBuffer)
        {
            cs.EnableKeyword(CSParam.Keyword_TrailIdxOn);
            cs.SetBuffer(kernel, CSParam.TrailIndexBuffer, trailIndexBuffer);
        }

        public static void SetComputeShaderParameterDisable(ComputeShader cs)
        {
            cs.DisableKeyword(CSParam.Keyword_TrailIdxOn);
        }

        #endregion



    }
}