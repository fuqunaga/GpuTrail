using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;

namespace GpuTrailSystem
{
    public class GpuTrailSingle : MonoBehaviour, IGpuTrailHolder
    {
        public GpuTrail gpuTrail;

        public ComputeShader _cs;
        public GraphicsBuffer _inputBuffer;

        public int totalInputIdx { get; protected set; } = -1;
        LinkedList<Vector3> _posLog = new LinkedList<Vector3>();

        public GpuTrail GpuTrail => gpuTrail;


        #region Unity


        void Start()
        {
            gpuTrail.Init();
            _inputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, gpuTrail.trailNum, Marshal.SizeOf(typeof(InputData)));

            _posLog.AddLast(transform.position);

        }

        void OnDestroy()
        {
            if (_inputBuffer != null) _inputBuffer.Release();

            gpuTrail?.Dispose();
        }

        #endregion


        /*
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
        */

        public void LateUpdate()
        {
            var pos = transform.position;
            var posPrev = _posLog.Last();

            if ((Vector3.Distance(posPrev, pos) > gpuTrail.minNodeDistance))
            {
                //var inputNum = Mathf.Clamp(Mathf.FloorToInt(Time.deltaTime * gpuTrail.inputPerSec), 1, _inputNumMax);
                //var inputNum = 1;

                /*
                if (inputNum > 1)
                {
                    LerpPos(inputNum, pos);
                }
                */

                var inputdata = new InputData()
                {
                    position = pos,
                    color = Color.white
                    //time = Time.time
                };

                _UpdateNode(inputdata);

                _posLog.AddLast(pos);

                // _posLogには過去２つの位置を保存しとく
                for (var i = 0; i < _posLog.Count - 2; ++i)
                {
                    _posLog.RemoveFirst();
                }
            }
        }

        void _UpdateNode(InputData inputData)
        {
            _inputBuffer.SetData(new[] { inputData });

            var kernel = _cs.FindKernel("AppendNode");
            _cs.SetBuffer(kernel, "_InputBuffer", _inputBuffer);
            gpuTrail.SetCSParams(_cs, kernel);

            ComputeShaderUtility.Dispatch(_cs, kernel, gpuTrail.trailBuffer.count);

            /*
            var trail = new Trail[gpuTrail.trailBuffer.count];
            gpuTrail.trailBuffer.GetData(trail);

            var node = new Node[gpuTrail.nodeBuffer.count];
            gpuTrail.nodeBuffer.GetData(node);
            */
        }


        #region Debug

        public bool _debugDrawLogPoint;

        public void OnDrawGizmosSelected()
        {
            if (_debugDrawLogPoint)
            {
                Gizmos.color = Color.magenta;
                foreach (var p in _posLog)
                {
                    Gizmos.DrawWireSphere(p, gpuTrail.minNodeDistance);
                }
            }
        }

        #endregion
    }
}