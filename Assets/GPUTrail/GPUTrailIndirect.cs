using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;

public class GPUTrailIndirect : GPUTrailBase
{
    struct Input
    {
        public Vector3 pos;
    }
    ComputeBuffer inputBuffer;
    Input[] inputDatas;

    public struct Trail
    {
        public float startTime;
        public int totalInputNum;
    }

    ComputeBuffer _trailBuffer;


    public int _trailNumMax = 1000;
    public int _nodeNumPerTrail = 1000;

    protected override int trailNumMax { get { return _trailNumMax; } }
    protected override int nodeNumPerTrail { get { return _nodeNumPerTrail; } }


    const float FPS = 60f;
    override protected void Awake()
    {
        _nodeNumPerTrail = Mathf.CeilToInt(_life * FPS);

        base.Awake();


        _trailBuffer = new ComputeBuffer(trailNumMax, Marshal.SizeOf(typeof(Trail)));
        _trailBuffer.SetData(Enumerable.Repeat(default(Trail), trailNumMax).ToArray());



        inputDatas = new Input[trailNumMax];
        for (var i = 0; i < inputDatas.Length; ++i)
        {
            inputDatas[i] = new Input()
            {
                pos = new Vector3(i*5f + 5f, 0f, 0f)
            };
        }
        inputBuffer = new ComputeBuffer(trailNumMax, Marshal.SizeOf(typeof(Input)));
    }

    override protected void ReleaseBuffer()
    {
        base.ReleaseBuffer();
        if (inputBuffer != null) inputBuffer.Release();
        if (_trailBuffer != null) _trailBuffer.Release();
    }

    const int NUM_THREAD_X = 16;
    protected override void UpdateVertex()
    {
        // AddNode
        for (var i = 0; i < inputDatas.Length; ++i)
        {
            var data = inputDatas[i];
            data.pos.z += 0.1f;
            inputDatas[i] = data;
        }
        inputBuffer.SetData(inputDatas);



        SetCommonParameterForCS();

        var kernel = cs.FindKernel("AddNode");
        cs.SetBuffer(kernel, "_InputBuffer", inputBuffer);
        cs.SetBuffer(kernel, "_TrailBufferW", _trailBuffer);
        cs.SetBuffer(kernel, "_NodeBufferW", _nodeBuffer);

        cs.Dispatch(kernel, Mathf.CeilToInt((float)_trailBuffer.count / NUM_THREAD_X), 1, 1);

        // CreateWidth
        kernel = cs.FindKernel("CreateWidth");
        cs.SetBuffer(kernel, "_TrailBuffer", _trailBuffer);
        cs.SetBuffer(kernel, "_NodeBuffer", _nodeBuffer);
        cs.SetBuffer(kernel, "_VertexBuffer", _vertexBuffer);
        cs.Dispatch(kernel, Mathf.CeilToInt((float)_nodeBuffer.count / NUM_THREAD_X), 1, 1);
    }
}
