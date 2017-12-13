using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;

public abstract class GPUTrailIndirect : GPUTrailBase
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

    protected ComputeBuffer _inputBuffer;
    ComputeBuffer _trailBuffer;


    override protected void Awake()
    {
        Assert.IsFalse(_lerpType == LerpType.Spline, "not implemented LerpType.Spline at GPUTrailIndirect.");

        base.Awake();

        _trailBuffer = new ComputeBuffer(trailNumMax, Marshal.SizeOf(typeof(Trail)));
        _trailBuffer.SetData(Enumerable.Repeat(default(Trail), trailNumMax).ToArray());

        _inputBuffer = new ComputeBuffer(trailNumMax, Marshal.SizeOf(typeof(InputData)));
    }

    override protected void ReleaseBuffer()
    {
        base.ReleaseBuffer();
        if (_inputBuffer != null) _inputBuffer.Release();
        if (_trailBuffer != null) _trailBuffer.Release();
    }

    const int NUM_THREAD_X = 32;
    protected override void UpdateVertex()
    {
        // AddNode
        SetCommonParameterForCS();

        UpdateInputBuffer();

        var kernel = _cs.FindKernel("AddNode");
        _cs.SetBuffer(kernel, "_InputBuffer", _inputBuffer);
        _cs.SetBuffer(kernel, "_TrailBufferW", _trailBuffer);
        _cs.SetBuffer(kernel, "_NodeBufferW", _nodeBuffer);

        _cs.Dispatch(kernel, Mathf.CeilToInt((float)_trailBuffer.count / NUM_THREAD_X), 1, 1);

        // CreateWidth
        kernel = _cs.FindKernel("CreateWidth");
        _cs.SetBuffer(kernel, "_TrailBuffer", _trailBuffer);
        _cs.SetBuffer(kernel, "_NodeBuffer", _nodeBuffer);
        _cs.SetBuffer(kernel, "_VertexBuffer", _vertexBuffer);
        _cs.Dispatch(kernel, Mathf.CeilToInt((float)_nodeBuffer.count / NUM_THREAD_X), 1, 1);
    }

    protected abstract void UpdateInputBuffer();
}
