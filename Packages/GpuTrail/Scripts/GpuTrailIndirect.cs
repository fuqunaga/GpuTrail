using UnityEngine;

namespace GpuTrailSystem
{

    public abstract class GpuTrailIndirect
    {
        public GpuTrail gpuTrail;
        public ComputeShader computeShader;
        protected GraphicsBuffer inputBuffer;
        

        protected virtual void Awake()
        {
            //_inputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, gpuTrail.trailNum, Marshal.SizeOf(typeof(InputData)));
        }

        protected virtual void OnDestroy()
        {
            if (inputBuffer != null) inputBuffer.Release();
            gpuTrail?.Dispose();
        }

        void LateUpdate()
        {
            // AddNode
            var success = UpdateInputBuffer();
            if (success)
            {
                var _trailBuffer = gpuTrail.trailBuffer;
                var nodeBuffer = gpuTrail.nodeBuffer;

                var kernel = computeShader.FindKernel("AddNode");
                computeShader.SetBuffer(kernel, "_InputBuffer", inputBuffer);
                computeShader.SetBuffer(kernel, "_TrailBufferW", _trailBuffer);
                computeShader.SetBuffer(kernel, "_NodeBufferW", nodeBuffer);
                ComputeShaderUtility.Dispatch(computeShader, kernel, nodeBuffer.count);

                // CreateWidth
                kernel = computeShader.FindKernel("CreateWidth");
                computeShader.SetBuffer(kernel, "_TrailBuffer", _trailBuffer);
                computeShader.SetBuffer(kernel, "_NodeBuffer", nodeBuffer);
                //_cs.SetBuffer(kernel, "_VertexBuffer", _vertexBuffer);
                ComputeShaderUtility.Dispatch(computeShader, kernel, nodeBuffer.count);
            }
        }

        protected abstract bool UpdateInputBuffer();
    }
}