using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace GpuTrailSystem
{
    public class GpuTrailRenderer_CalcLod : IDisposable
    {
        public static class CSParam
        {
            public static readonly string Kernel_UpdateTrailLodBuffer = "UpdateTrailLodBuffer";
            public static readonly int CameraPos = Shader.PropertyToID("_CameraPos");
            public static readonly int LodDistanceBuffer = Shader.PropertyToID("_LodDistanceBuffer");
            public static readonly int TrailLodBufferW = Shader.PropertyToID("_TrailLodBufferW");

            public static readonly string Kernel_UpdateTrailIndexBuffer = "UpdateTrailIndexBuffer";
            public static readonly int CurrentLod = Shader.PropertyToID("_CurrentLod");
            public static readonly int TrailLodBuffer = Shader.PropertyToID("_TrailLodBuffer");
            public static readonly int TrailIdxBufferAppend = Shader.PropertyToID("_TrailIdxBufferAppend");
        }


        protected ComputeShader calcLodCS;

        protected GraphicsBuffer lodDistanceBuffer;
        protected GraphicsBuffer trailLodBuffer;
        protected List<GraphicsBuffer> trailIndexBuffers = new List<GraphicsBuffer>();

        protected readonly GpuTrailIndexDispatcher gpuTrailIndexArgs = new GpuTrailIndexDispatcher();



        public GpuTrailRenderer_CalcLod(ComputeShader calcLodCS) => this.calcLodCS = calcLodCS;


        public void Dispose()
        {
            ReleaseBuffers();
            gpuTrailIndexArgs.Dispose();
        }


        void ResetBuffers(int trailNum, int lodNum)
        {
            ReleaseBuffers();
            lodDistanceBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, lodNum, sizeof(float));
            trailLodBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, trailNum, sizeof(uint));

            trailIndexBuffers = Enumerable.Range(0, lodNum)
                .Select(_ => new GraphicsBuffer(GraphicsBuffer.Target.Append, trailNum, sizeof(uint)))
                .ToList();
        }

        void ReleaseBuffers()
        {
            lodDistanceBuffer?.Release();
            trailLodBuffer?.Release();

            lodDistanceBuffer = null;
            trailLodBuffer = null;

            foreach (var buf in trailIndexBuffers) buf?.Release();
            trailIndexBuffers.Clear();
        }


        // return TrailIndexBuffer in the same order as the lodDistances
        public virtual IReadOnlyList<GraphicsBuffer> CalcTrailIndexBuffers(IEnumerable<float> lodDistances, Camera camera, GpuTrail gpuTrail, GraphicsBuffer trailIndexBuffer)
        {
            var idxAndDistances = lodDistances
                .Select((distance, idx) => (idx, distance))
                .OrderBy(pair => pair.distance)
                .ToList();

            if (lodDistanceBuffer == null || lodDistanceBuffer.count != idxAndDistances.Count)
            {
                ResetBuffers(gpuTrail.trailNum, idxAndDistances.Count);
            }

            UpdateTrailLodBuffer(
                 idxAndDistances.Select(pair => pair.distance).ToArray(),
                 camera, gpuTrail, trailIndexBuffer
                );

            UpdateTrailIndexBuffers(
                idxAndDistances.Select(pair => pair.idx),
                gpuTrail.trailNum,
                trailIndexBuffer
                );

            return trailIndexBuffers;
        }


        protected void UpdateTrailLodBuffer(float[] sortedDistances, Camera camera, GpuTrail gpuTrail, GraphicsBuffer trailIndexBuffer)
        {
            lodDistanceBuffer.SetData(sortedDistances);

            var kernel = calcLodCS.FindKernel(CSParam.Kernel_UpdateTrailLodBuffer);
            gpuTrail.SetCSParams(calcLodCS, kernel);
            calcLodCS.SetVector(CSParam.CameraPos, camera.transform.position);
            calcLodCS.SetBuffer(kernel, CSParam.LodDistanceBuffer, lodDistanceBuffer);
            calcLodCS.SetBuffer(kernel, CSParam.TrailLodBufferW, trailLodBuffer);

            if (trailIndexBuffer != null)
            {
                gpuTrailIndexArgs.Dispatch(calcLodCS, kernel, trailIndexBuffer);
            }
            else
            {
                gpuTrailIndexArgs.Dispatch(calcLodCS, kernel, gpuTrail.trailNum);
            }
        }

        protected void UpdateTrailIndexBuffers(IEnumerable<int> idxSequence, int trailNum, GraphicsBuffer trailIndexBufferForAll)
        {
            var kernel = calcLodCS.FindKernel(CSParam.Kernel_UpdateTrailIndexBuffer);

            foreach (var idx in idxSequence)
            {
                var trailIndexBufferForLod = trailIndexBuffers[idx];
                trailIndexBufferForLod.SetCounterValue(0);

                calcLodCS.SetInt(CSParam.CurrentLod, idx);
                calcLodCS.SetBuffer(kernel, CSParam.TrailLodBuffer, trailLodBuffer);
                calcLodCS.SetBuffer(kernel, CSParam.TrailIdxBufferAppend, trailIndexBufferForLod);

                //ComputeShaderUtility.Dispatch(calcLodCS, kernel, trailNum);

                if (trailIndexBufferForAll != null)
                {
                    gpuTrailIndexArgs.Dispatch(calcLodCS, kernel, trailIndexBufferForAll);
                }
                else
                {
                    gpuTrailIndexArgs.Dispatch(calcLodCS, kernel, trailNum);
                }
            }

            /*
            var trailLods = new uint[trailLodBuffer.count];
            trailLodBuffer.GetData(trailLods);

            var idxs = trailIndexBuffers.Select(buf =>
            {
                var datas = new uint[buf.count];
                buf.GetData(datas);
                return datas;
            }).ToList();
            */
        }
    }
}