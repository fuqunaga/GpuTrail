using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;

public class GPUTrail : GPUTrailBase
{

    #region TypeDefine

    public enum LerpMode
    {
        Linear,
        Spline
    }

    #endregion


    public float InputPerSec = 60f;
    public int InputNumMax = 5;
    public LerpMode _lerpMode = LerpMode.Spline;
    public float _minVertexDistance = 0.1f;

    LinkedList<Vector3> _posLog = new LinkedList<Vector3>();
    int _nodeNum;
    int _totalInputIdx = -1;

    ComputeBuffer _inputBuffer;



    protected override int trailNumMax { get { return 1; } }
    protected override int nodeNumPerTrail { get { return _nodeNum; } }

    protected override void Awake()
    {
        _nodeNum = Mathf.CeilToInt(_life * InputPerSec);

        base.Awake();

        _inputBuffer = new ComputeBuffer(InputNumMax, Marshal.SizeOf(typeof(Node)));
    }


    protected override void Start()
    {
        base.Start();
        _posLog.AddLast(transform.position);
    }

    protected override void ReleaseBuffer()
    {
        base.ReleaseBuffer();
        if (_inputBuffer != null) _inputBuffer.Release();
    }


    void _LerpPos(int inputNum, Vector3 pos)
    {
        var timeStep = Time.deltaTime / inputNum;
        var timePrev = Time.time - Time.deltaTime;

        if (LerpMode.Spline == _lerpMode && (_posLog.Count >= 2))
        {
            var prev = _posLog.Last.Previous.Value;
            var start = _posLog.Last.Value;

            for (var i = 1; i < inputNum; ++i)
            {
                _newPoints.Add(new Node()
                {
                    pos = GPUTrailSpline.CatmullRom((float)i / inputNum, prev, start, pos),
                    time = timePrev + timeStep * i
                });
            }
        }
        // Linear
        else
        {
            var posPrev = _posLog.Last();
            var posStep = (pos - posPrev) / inputNum;

            for (var i = 1; i < inputNum; ++i)
            {
                _newPoints.Add(new Node()
                {
                    pos = posPrev + posStep * i,
                    time = timePrev + timeStep * i
                });
            }
        }
    }

    List<Node> _newPoints = new List<Node>();
    protected override void UpdateVertex()
    {
        var pos = transform.position;
        var posPrev = _posLog.Last();

        if ((Vector3.Distance(posPrev, pos) > _minVertexDistance))
        {
            var inputNum = Mathf.Clamp(Mathf.FloorToInt(Time.deltaTime * InputPerSec), 1, InputNumMax);
            //inputNum = 1;

            if (inputNum > 1)
            {
                _LerpPos(inputNum, pos);
            }

            _newPoints.Add(new Node()
            {
                pos = pos,
                time = Time.time
            });

            _posLog.AddLast(pos);

            // _posLogには過去２つの位置を保存しとく
            for (var i = 0; i < _posLog.Count - 2; ++i)
            {
                _posLog.RemoveFirst();
            }
        }

        _UpdateVertex(_newPoints);

        _newPoints.Clear();
    }

    const int NUM_THREAD_X = 16;
    void _UpdateVertex(List<Node> newPoints)
    {
        Assert.IsTrue(newPoints.Count <= InputNumMax);

        var inputNum = newPoints.Count;
        if (inputNum > 0)
        {
            _inputBuffer.SetData(newPoints.ToArray());
            if (_totalInputIdx < 0) _startTime = Time.time;
            _totalInputIdx += inputNum;
        }

        if (_totalInputIdx >= 0)
        {
            SetCommonParameterForCS();

            cs.SetInt("_InputNum", inputNum);
            cs.SetInt("_TotalInputIdx", _totalInputIdx);
            cs.SetInt("_BufferSize", nodeNumPerTrail);

            var kernel = cs.FindKernel("CreateWidth");
            cs.SetBuffer(kernel, "inputBuffer", _inputBuffer);
            cs.SetBuffer(kernel, "pointBuffer", _nodeBuffer);
            cs.SetBuffer(kernel, "vertexBuffer", _vertexBuffer);

            cs.Dispatch(kernel, Mathf.CeilToInt((float)_nodeBuffer.count / NUM_THREAD_X), 1, 1);
        }
    }
}
