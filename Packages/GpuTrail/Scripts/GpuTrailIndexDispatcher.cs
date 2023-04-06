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

        public static class CsParam
        {
            public const string KernelCalcArgsBufferForCs = "CalcArgsBufferForCS";
            public static readonly int ThreadGroupSizeX = Shader.PropertyToID("_ThreadGroupSizeX");
            public static readonly int TotalThreadNum = Shader.PropertyToID("_TotalThreadNum");
            // ReSharper disable once InconsistentNaming
            public static readonly int ArgsBufferForCS = Shader.PropertyToID("_ArgsBufferForCS");
        }

        public static class CsIncludeParam
        {
            public const string KeywordTrailIdxOn = "GPUTRAIL_TRAIL_INDEX_ON";
            public static readonly int TrailIndexBuffer = Shader.PropertyToID("_TrailIndexBuffer");
            public static readonly int TrailNumBuffer = Shader.PropertyToID("_TrailNumBuffer");
        }

        static ComputeShader _computeShader;

        public static void Init(ComputeShader computeShader) => _computeShader = computeShader;

        #endregion
        
        private GraphicsBuffer _totalThreadNumBuffer;
        private GraphicsBuffer _argsBuffer;

        public void InitBuffers()
        {
            ReleaseBuffers();

            _totalThreadNumBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, 1, sizeof(uint));
            _argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 3, sizeof(uint));
            _argsBuffer.SetData(new[] { 1, 1, 1 });
        }

        public void ReleaseBuffers()
        {
            _totalThreadNumBuffer?.Release();
            _argsBuffer?.Release();

            _totalThreadNumBuffer = null;
            _argsBuffer = null;
        }

        public void Dispose() => ReleaseBuffers();


        public void Dispatch(ComputeShader cs, int kernel, int trailNum) => _Dispatch(cs, kernel, trailNum, null);

        public void Dispatch(ComputeShader cs, int kernel, GraphicsBuffer trailIndexBuffer) => _Dispatch(cs, kernel, 0, trailIndexBuffer);

        private void _Dispatch(ComputeShader cs, int kernel, int trailNum, GraphicsBuffer trailIndexBuffer) 
        {
            if (_totalThreadNumBuffer == null) InitBuffers();

            var threadGroupSizeX = GetThreadGroupSizeX(cs, kernel);

            if (trailIndexBuffer != null)
            {
                GraphicsBuffer.CopyCount(trailIndexBuffer, _totalThreadNumBuffer, 0);

                UpdateArgsBuffer(trailIndexBuffer, threadGroupSizeX);
                SetComputeShaderParameterEnable(cs, kernel, trailIndexBuffer, _totalThreadNumBuffer);
            }
            else
            {
                UpdateArgsBuffer(trailNum, threadGroupSizeX);
                SetComputeShaderParameterDisable(cs);
            }

            cs.DispatchIndirect(kernel, _argsBuffer);
        }


        private static int GetThreadGroupSizeX(ComputeShader cs, int kernel)
        {
            cs.GetKernelThreadGroupSizes(kernel, out var x, out var _, out var _);
            return (int)x;
        }

        private void UpdateArgsBuffer(int trailNum, int threadGroupSizeX)
        {
            _totalThreadNumBuffer.SetData(new[] { trailNum });
            _argsBuffer.SetData(new[] { Mathf.CeilToInt((float)trailNum / threadGroupSizeX), 1, 1 });
        }

        public void UpdateArgsBuffer(GraphicsBuffer trailIndexBuffer, int threadGroupSizeX)
        {
            var kernel = _computeShader.FindKernel(CsParam.KernelCalcArgsBufferForCs);
            _computeShader.SetInt(CsParam.ThreadGroupSizeX, threadGroupSizeX);
            _computeShader.SetBuffer(kernel, CsParam.TotalThreadNum, _totalThreadNumBuffer);
            _computeShader.SetBuffer(kernel, CsParam.ArgsBufferForCS, _argsBuffer);

            _computeShader.Dispatch(kernel, 1, 1, 1);
        }


        public static void SetComputeShaderParameterEnable(ComputeShader cs, int kernel, GraphicsBuffer trailIndexBuffer, GraphicsBuffer trailNumBuffer)
        {
            cs.EnableKeyword(CsIncludeParam.KeywordTrailIdxOn);
            cs.SetBuffer(kernel, CsIncludeParam.TrailIndexBuffer, trailIndexBuffer);
            cs.SetBuffer(kernel, CsIncludeParam.TrailNumBuffer, trailNumBuffer);
        }

        public static void SetComputeShaderParameterDisable(ComputeShader cs)
        {
            cs.DisableKeyword(CsIncludeParam.KeywordTrailIdxOn);
        }

    }
}