using System;
using UnityEngine;


namespace GpuTrailSystem
{
    /// <summary>
    /// C# side corresponding to GpuTrailIndexInclude.cginc
    /// </summary>
    public class GpuTrailIndexDispatcher : IDisposable
    {
        #region Static

        public static class CSParam
        {
            public static readonly string Kernel_CalcArgsBufferForCS = "CalcArgsBufferForCS";
            public static readonly int ThreadGroupSizeX = Shader.PropertyToID("_ThreadGroupSizeX");
            public static readonly int TotalThreadNum = Shader.PropertyToID("_TotalThreadNum");
            public static readonly int ArgsBufferForCS = Shader.PropertyToID("_ArgsBufferForCS");
        }

        public static class CSIncludeParam
        { 
            public static readonly string Keyword_TrailIdxOn = "GPUTRAIL_TRAIL_INDEX_ON";
            public static readonly int TrailIndexBuffer = Shader.PropertyToID("_TrailIndexBuffer");
            public static readonly int TrailNumBuffer = Shader.PropertyToID("_TrailNumBuffer");
        }

        static ComputeShader _computeShader;

        public static void Init(ComputeShader computeShader) => _computeShader = computeShader;

        #endregion


        GraphicsBuffer totalThreadNumBuffer;
        GraphicsBuffer argsBuffer;



        public void InitBuffers()
        {
            ReleaseBuffers();

            totalThreadNumBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, 1, sizeof(uint));
            argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 3, sizeof(uint));
            argsBuffer.SetData(new[] { 1, 1, 1 });
        }

        public void ReleaseBuffers()
        {
            totalThreadNumBuffer?.Release();
            argsBuffer?.Release();

            totalThreadNumBuffer = null;
            argsBuffer = null;
        }

        public void Dispose() => ReleaseBuffers();



        public void Dispatch(ComputeShader cs, int kernel, int trailNum) => _Dispatch(cs, kernel, trailNum, null);

        public void Dispatch(ComputeShader cs, int kernel, GraphicsBuffer trailIndexBuffer) => _Dispatch(cs, kernel, 0, trailIndexBuffer);

        void _Dispatch(ComputeShader cs, int kernel, int trailNum, GraphicsBuffer trailIndexBuffer) 
        {
            if (totalThreadNumBuffer == null) InitBuffers();

            var threadGroupSizeX = GetThreadGroupSizeX(cs, kernel);

            if (trailIndexBuffer != null)
            {
                GraphicsBuffer.CopyCount(trailIndexBuffer, totalThreadNumBuffer, 0);

                UpdateArgsBuffer(trailIndexBuffer, threadGroupSizeX);
                SetComputeShaderParameterEnable(cs, kernel, trailIndexBuffer, totalThreadNumBuffer);
            }
            else
            {
                UpdateArgsBuffer(trailNum, threadGroupSizeX);
                SetComputeShaderParameterDisable(cs);
            }

            cs.DispatchIndirect(kernel, argsBuffer);
        }


        int GetThreadGroupSizeX(ComputeShader cs, int kernel)
        {
            cs.GetKernelThreadGroupSizes(kernel, out var x, out var _, out var _);
            return (int)x;
        }

        void UpdateArgsBuffer(int trailNum, int threadGroupSizeX)
        {
            totalThreadNumBuffer.SetData(new[] { trailNum });
            argsBuffer.SetData(new[] { Mathf.CeilToInt((float)trailNum / threadGroupSizeX), 1, 1 });
        }

        public void UpdateArgsBuffer(GraphicsBuffer trailIndexBuffer, int threadGroupSizeX)
        {
            var kernel = _computeShader.FindKernel(CSParam.Kernel_CalcArgsBufferForCS);
            _computeShader.SetInt(CSParam.ThreadGroupSizeX, threadGroupSizeX);
            _computeShader.SetBuffer(kernel, CSParam.TotalThreadNum, totalThreadNumBuffer);
            _computeShader.SetBuffer(kernel, CSParam.ArgsBufferForCS, argsBuffer);

            _computeShader.Dispatch(kernel, 1, 1, 1);
        }


        public static void SetComputeShaderParameterEnable(ComputeShader cs, int kernel, GraphicsBuffer trailIndexBuffer, GraphicsBuffer trailNumBuffer)
        {
            cs.EnableKeyword(CSIncludeParam.Keyword_TrailIdxOn);
            cs.SetBuffer(kernel, CSIncludeParam.TrailIndexBuffer, trailIndexBuffer);
            cs.SetBuffer(kernel, CSIncludeParam.TrailNumBuffer, trailNumBuffer);
        }

        public static void SetComputeShaderParameterDisable(ComputeShader cs)
        {
            cs.DisableKeyword(CSIncludeParam.Keyword_TrailIdxOn);
        }

    }
}