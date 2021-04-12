using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace GpuTrailSystem
{
    [Serializable]
    public class GpuTrailCalcLod : MonoBehaviour
    {
        public static class CSParam
        {
            
            //public static readonly string Keyword_TrailIdxOn = "GPUTRAIL_TRAIL_INDEX_ON";
            public static readonly string Kernel_CalcLod = "CalcLod";
            public static readonly int CameraPos = Shader.PropertyToID("_CameraPos");
            public static readonly int LodDistanceBuffer = Shader.PropertyToID("_LodDistanceBuffer");
            public static readonly int TrailLodBuffer = Shader.PropertyToID("_TrailLodBuffer");
        }


        public ComputeShader calcLodCS;

        protected GraphicsBuffer lodDistanceBuffer;
        protected GraphicsBuffer trailLodBuffer;

        void OnDestroy()
        {
            ReleaseBuffers();
        }


        void InitBuffers(int trailNum, int lodNum)
        {
            lodDistanceBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, lodNum, sizeof(float));
            trailLodBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, trailNum, sizeof(uint));
        }

        void ReleaseBuffers()
        {
            if (lodDistanceBuffer != null) lodDistanceBuffer.Release();
            if (trailLodBuffer != null) trailLodBuffer.Release();
        }

        public virtual void CalcLod(Camera camera, GpuTrail gpuTrail, IReadOnlyList<GpuTrailRenderer.LodSetting> lods)
        {
            if (lodDistanceBuffer == null || lodDistanceBuffer.count != lods.Count)
            {
                InitBuffers(gpuTrail.trailNum, lods.Count);
            }

            var distances = lods.Select(lod => lod.distance).OrderBy(distance => distance).ToArray();
            lodDistanceBuffer.SetData(distances);

            var kernel = calcLodCS.FindKernel(CSParam.Kernel_CalcLod);
            gpuTrail.SetCSParams(calcLodCS, kernel);
            calcLodCS.SetVector(CSParam.CameraPos, camera.transform.position);
            calcLodCS.SetBuffer(kernel, CSParam.LodDistanceBuffer, lodDistanceBuffer);
            calcLodCS.SetBuffer(kernel, CSParam.TrailLodBuffer, trailLodBuffer);

            ComputeShaderUtility.Dispatch(calcLodCS, kernel, gpuTrail.trailNum);

#if false
            var trailLods = new uint[trailLodBuffer.count];
            trailLodBuffer.GetData(trailLods);
            Debug.Log(string.Join(",", trailLods.Take(100).ToArray()));
#endif
        }



#if false
        public void SetComputeShaderParameterEnable(ComputeShader cs, int kernel)
        {
            cs.EnableKeyword(CSParam.Keyword_TrailIdxOn);
            cs.SetBuffer(kernel, CSParam.TrailIndexBuffer, trailIndexBuffer);
        }

        public void SetComputeShaderParameterDisable(ComputeShader cs)
        {
            cs.DisableKeyword(CSParam.Keyword_TrailIdxOn);
        }

        public static void SetComputeShaderParameterDisableDefault(ComputeShader cs)
        {
            cs.DisableKeyword(CSParam.Keyword_TrailIdxOn);
        }
#endif
    }
}