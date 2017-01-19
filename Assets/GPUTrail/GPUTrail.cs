using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;

public class GPUTrail : MonoBehaviour
{

    #region TypeDefine

    public enum LerpMode
    {
        Linear,
        Spline
    }

    public struct Point
    {
        public Vector3 pos;
        public float time;
    }

    public struct Vertex
    {
        public Vector3 pos;
        public Vector2 uv;
    }

    #endregion


    [Header("StaticParameter")]
    public ComputeShader cs;
    public float InputPerSec = 60f;
    public int InputNumMax = 5;
    public LerpMode _lerpMode = LerpMode.Spline;

    [Header("Parameter")]
    public float _life = 1f;
    public float _startWidth = 1f;
    public float _endWidth = 1f;
    public Color _startColor = Color.white;
    public Color _endColor = Color.white;
    public float _minVertexDistance = 0.1f;
    public Material _material;

    LinkedList<Vector3> _posLog = new LinkedList<Vector3>();
    int _bufferSize;
    int _totalInputIdx = -1;
    float _startTime;

    ComputeBuffer _inputBuffer;
    ComputeBuffer _pointBuffer;
    ComputeBuffer _vertexBuffer;
    ComputeBuffer _indexBuffer;


    void Awake()
    {
        ReleaseBuffer();

        _bufferSize = Mathf.CeilToInt(_life * InputPerSec);

        _inputBuffer = new ComputeBuffer(InputNumMax, Marshal.SizeOf(typeof(Point)));
        _pointBuffer = new ComputeBuffer(_bufferSize, Marshal.SizeOf(typeof(Point)));

        _vertexBuffer = new ComputeBuffer(_bufferSize * 2, Marshal.SizeOf(typeof(Vertex))); // 1 input to 2 triangles(6vertexs)


        // 各Nodeの最後と次のNodeの最初はポリゴンを繋がないので-1
        var nodeNumPerTrail = _bufferSize;
        var indexData = new int[(_bufferSize - 1) * 6];
        var iidx = 0;
        var trailNumMax = 1;
        for (var iTrail = 0; iTrail < trailNumMax; ++iTrail)
        {
            var nodeStart = iTrail * nodeNumPerTrail * 2;
            for (var iNode = 0; iNode < nodeNumPerTrail - 1; ++iNode)
            {
                var offset = nodeStart + iNode * 2;
                indexData[iidx++] = 0 + offset;
                indexData[iidx++] = 1 + offset;
                indexData[iidx++] = 2 + offset;
                indexData[iidx++] = 2 + offset;
                indexData[iidx++] = 1 + offset;
                indexData[iidx++] = 3 + offset;
            }
        }

        _indexBuffer = new ComputeBuffer(indexData.Length, Marshal.SizeOf(typeof(uint))); // 1 node to 2 triangles(6vertexs)
        _indexBuffer.SetData(indexData);
    }

    void Start()
    {
        _posLog.AddLast(transform.position);
        _startTime = Time.time;
    }

    void ReleaseBuffer()
    {
        new[] { _inputBuffer, _pointBuffer, _vertexBuffer }
            .Where(b => b != null)
            .ToList().ForEach(buffer =>
            {
                buffer.Release();
                buffer = null;
            });
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
                _newPoints.Add(new Point()
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
                _newPoints.Add(new Point()
                {
                    pos = posPrev + posStep * i,
                    time = timePrev + timeStep * i
                });
            }
        }
    }

    List<Point> _newPoints = new List<Point>();
    void LateUpdate()
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

            _newPoints.Add(new Point()
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

        UpdateVertex(_newPoints);

        _newPoints.Clear();
    }

    protected virtual Vector3 cameraPos { get { return Camera.main.transform.position; } }
    const int NUM_THREAD_X = 16;
    void UpdateVertex(List<Point> newPoints)
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
            var cPos = cameraPos;
            cs.SetFloats("_CameraPos", cPos.x, cPos.y, cPos.z);
            cs.SetFloat("_Life", Mathf.Min(_life, Time.time - _startTime));
            cs.SetInt("_InputNum", inputNum);
            cs.SetInt("_TotalInputIdx", _totalInputIdx);
            cs.SetInt("_BufferSize", _bufferSize);
            cs.SetFloat("_Time", Time.time);
            cs.SetFloat("_StartWidth", _startWidth);
            cs.SetFloat("_EndWidth", _endWidth);
            cs.SetFloats("_StartColor", _startColor.r, _startColor.g, _startColor.b, _startColor.a);
            cs.SetFloats("_EndColor", _endColor.r, _endColor.g, _endColor.b, _endColor.a);

            var kernel = cs.FindKernel("CreateWidth");
            cs.SetBuffer(kernel, "inputBuffer", _inputBuffer);
            cs.SetBuffer(kernel, "pointBuffer", _pointBuffer);
            cs.SetBuffer(kernel, "vertexBuffer", _vertexBuffer);

            cs.Dispatch(kernel, Mathf.CeilToInt((float)_bufferSize / NUM_THREAD_X), 1, 1);


            /*
            kernel = cs.FindKernel("CreatePolygon");
            cs.SetBuffer(kernel, "pointBuffer", _pointBuffer);
            cs.SetBuffer(kernel, "vertexBuffer", _vertexBuffer);
            cs.Dispatch(kernel, Mathf.CeilToInt((float)_bufferSize / NUM_THREAD_X), 1, 1);
            */
        }
    }

    void OnRenderObject()
    {
        if ((Camera.current.cullingMask & (1 << gameObject.layer)) == 0)
        {
            return;
        }

        if (_totalInputIdx >= 2)
        {
            setMaterilParam();
            _material.SetBuffer("_Indexes", _indexBuffer);
            _material.SetBuffer("vertexBuffer", _vertexBuffer);
            _material.SetPass(0);

            /*
            var drawPointNum = Mathf.Min(_bufferSize, _totalInputIdx) - 1;
            var vertexCount = drawPointNum * 6;
            */
            Graphics.DrawProcedural(MeshTopology.Triangles, _indexBuffer.count);

            /*
            _material.SetPass(1);
            Graphics.DrawProcedural(MeshTopology.Triangles, vertexCount);
            */
        }
    }

    protected virtual void setMaterilParam() { }

    public void OnDestroy()
    {
        ReleaseBuffer();
    }
}
