using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GpuTrailSystem
{
    public struct InputData
    {
        public Vector3 position;
        public Color color;
    }

    public abstract class GpuTrailIndirect
    {
        public GpuTrail gpuTrail;
        public ComputeShader _cs;
        protected GraphicsBuffer _inputBuffer;
        

        protected virtual void Awake()
        {
            _inputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, gpuTrail.trailNum, Marshal.SizeOf(typeof(InputData)));
        }

        protected virtual void OnDestroy()
        {
            if (_inputBuffer != null) _inputBuffer.Release();
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

                var kernel = _cs.FindKernel("AddNode");
                _cs.SetBuffer(kernel, "_InputBuffer", _inputBuffer);
                _cs.SetBuffer(kernel, "_TrailBufferW", _trailBuffer);
                _cs.SetBuffer(kernel, "_NodeBufferW", nodeBuffer);
                ComputeShaderUtility.Dispatch(_cs, kernel, nodeBuffer.count);

                // CreateWidth
                kernel = _cs.FindKernel("CreateWidth");
                _cs.SetBuffer(kernel, "_TrailBuffer", _trailBuffer);
                _cs.SetBuffer(kernel, "_NodeBuffer", nodeBuffer);
                //_cs.SetBuffer(kernel, "_VertexBuffer", _vertexBuffer);
                ComputeShaderUtility.Dispatch(_cs, kernel, nodeBuffer.count);
            }
        }

        protected abstract bool UpdateInputBuffer();
    }
}