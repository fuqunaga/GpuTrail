using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GpuTrailSystem
{
    public class GpuTrailSingle : MonoBehaviour, IGpuTrailHolder
    {
        public GpuTrail gpuTrail;
        public GpuTrail GpuTrail => gpuTrail;

        public ComputeShader _cs;
        
        LinkedList<Vector3> posLog = new LinkedList<Vector3>();


        #region Unity


        void Start()
        {
            gpuTrail.Init();
            posLog.AddLast(transform.position);

        }

        void OnDestroy()
        {
            gpuTrail?.Dispose();
        }



        public void LateUpdate()
        {
            var pos = transform.position;
            var posPrev = posLog.Last();

            if ((Vector3.Distance(posPrev, pos) > gpuTrail.minNodeDistance))
            {
                UpdateNode(pos);

                posLog.AddLast(pos);

                // _posLogには過去２つの位置を保存しとく
                for (var i = 0; i < posLog.Count - 2; ++i)
                {
                    posLog.RemoveFirst();
                }
            }
        }

        #endregion


        void UpdateNode(Vector3 inputPos)
        {
            gpuTrail.inputBuffer_Pos.SetData(new[] { inputPos });

            gpuTrail.DispatchAppendNode();

            /*
            var kernel = _cs.FindKernel("AppendNode");
            gpuTrail.SetCSParams(_cs, kernel);

            ComputeShaderUtility.Dispatch(_cs, kernel, gpuTrail.trailBuffer.count);
            */

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
                foreach (var p in posLog)
                {
                    Gizmos.DrawWireSphere(p, gpuTrail.minNodeDistance);
                }
            }
        }

        #endregion
    }
}