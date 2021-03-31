using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;

namespace GpuTrailSystem
{
    public class GpuTrailSingle : GpuTrail
    {
        public int totalInputIdx { get; protected set; } = -1;

        public GraphicsBuffer _inputBuffer;
        public float _minNodeDistance = 0.1f;
        public int _inputNumMax = 5;

        LinkedList<Vector3> _posLog = new LinkedList<Vector3>();

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
        protected override void UpdateNode()
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

            _UpdateNode(_newPoints);

            _newPoints.Clear();
        }

        void _UpdateNode(List<Node> newPoints)
        {
            Assert.IsTrue(newPoints.Count <= _inputNumMax);

            var inputNum = newPoints.Count;
            if (inputNum > 0)
            {
                _inputBuffer.SetData(newPoints.ToArray());
                totalInputIdx += inputNum;
            }

            if (totalInputIdx >= 0)
            {
                _cs.SetInt("_InputNum", inputNum);
                _cs.SetInt("_TotalInputIdx", totalInputIdx);

                var kernel = _cs.FindKernel("AppendNode");
                _cs.SetBuffer(kernel, "_InputBuffer", _inputBuffer);
                _cs.SetBuffer(kernel, "_NodeBuffer", nodeBuffer);

                ComputeShaderUtility.Dispatch(_cs, kernel, nodeBuffer.count);
            }
        }


        #region Debug

        public bool _debugDrawLogPoint;

        public void OnDrawGizmosSelected()
        {
            if (_debugDrawLogPoint)
            {
                Gizmos.color = Color.magenta;
                foreach( var p in _posLog)
                {
                    Gizmos.DrawWireSphere(p, _minNodeDistance);
                }
            }
        }

        #endregion
    }
}