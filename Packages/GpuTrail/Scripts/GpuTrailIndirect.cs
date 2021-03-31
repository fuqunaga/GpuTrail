using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GpuTrailSystem
{
    public abstract class GpuTrailIndirect : GpuTrail
    {
        #region TypeDefine
        public struct Trail
        {
            public float startTime;
            public int totalInputNum;
        }


        public struct InputData
        {
            public Vector3 position;
            public Color color;
        }
        #endregion

        protected GraphicsBuffer _inputBuffer;
        GraphicsBuffer _trailBuffer;


        override protected void Awake()
        {
            base.Awake();

            _trailBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, trailNumMax, Marshal.SizeOf(typeof(Trail)));
            _trailBuffer.SetData(Enumerable.Repeat(default(Trail), trailNumMax).ToArray());

            _inputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, trailNumMax, Marshal.SizeOf(typeof(InputData)));
        }

        override protected void ReleaseBuffer()
        {
            base.ReleaseBuffer();
            if (_inputBuffer != null) _inputBuffer.Release();
            if (_trailBuffer != null) _trailBuffer.Release();
        }

        protected override void UpdateVertex()
        {
            // AddNode
            SetCommonParameterForCS();

            var success = UpdateInputBuffer();
            if (success)
            {
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