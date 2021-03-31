using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;

namespace GpuTrailSystem
{
    public class GpuTrailSingle : GpuTrail
    {
        LinkedList<Vector3> _posLog = new LinkedList<Vector3>();
        int _totalInputIdx = -1;

        public GraphicsBuffer _inputBuffer;

        protected float _startTime;

        public override int trailNumMax => 1;


        #region Unity

        protected override void Awake()
        {
            base.Awake();

            _inputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _inputNumMax, Marshal.SizeOf(typeof(Node)));
        }


        void Start()
        {
            _posLog.AddLast(transform.position);
        }

        #endregion


        protected override void ReleaseBuffer()
        {
            base.ReleaseBuffer();
            if (_inputBuffer != null) _inputBuffer.Release();
        }


        void LerpPos(int inputNum, Vector3 pos)
        {
            var timeStep = Time.deltaTime / inputNum;
            var timePrev = Time.time - Time.deltaTime;

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

        List<Node> _newPoints = new List<Node>();
        protected override void UpdateVertex()
        {
            var pos = transform.position;
            var posPrev = _posLog.Last();

            if ((Vector3.Distance(posPrev, pos) > _minNodeDistance))
            {
                var inputNum = Mathf.Clamp(Mathf.FloorToInt(Time.deltaTime * _inputPerSec), 1, _inputNumMax);
                //inputNum = 1;

                if (inputNum > 1)
                {
                    LerpPos(inputNum, pos);
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
            Assert.IsTrue(newPoints.Count <= _inputNumMax);

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

                _cs.SetInt("_InputNum", inputNum);
                _cs.SetInt("_TotalInputIdx", _totalInputIdx);
                _cs.SetInt("_BufferSize", nodeNumPerTrail);
                _cs.SetFloat("_StartTime", _startTime);

                //var kernel = _cs.FindKernel("CreateWidth");
                var kernel = _cs.FindKernel("AppendNode");
                _cs.SetBuffer(kernel, "_InputBuffer", _inputBuffer);
                _cs.SetBuffer(kernel, "_NodeBuffer", nodeBuffer);
                //_cs.SetBuffer(kernel, "_VertexBuffer", _vertexBuffer);

                ComputeShaderUtility.Dispatch(_cs, kernel, nodeBuffer.count);
            }
        }


        public bool _debugDrawLogPoint;
        //public bool _debugDrawVertexBuf;

        public void OnDrawGizmosSelected()
        {
            if (_debugDrawLogPoint)
            {
                Gizmos.color = Color.magenta;
                _posLog.ToList().ForEach(p =>
                {
                    Gizmos.DrawWireSphere(p, _minNodeDistance);
                });
            }

            /*
            if (_debugDrawVertexBuf)
            {
                Gizmos.color = Color.yellow;
                var data = new Vertex[_vertexBuffer.count];
                _vertexBuffer.GetData(data);

                var num = _vertexBuffer.count / 2;
                for (var i = 0; i < num; ++i)
                {
                    var v0 = data[2 * i];
                    var v1 = data[2 * i + 1];

                    Gizmos.DrawLine(v0.pos, v1.pos);
                }
            }
            */
        }
    }
}