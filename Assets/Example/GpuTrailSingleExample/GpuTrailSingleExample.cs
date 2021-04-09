using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GpuTrailSystem.Example
{
    public class GpuTrailSingleExample : GpuTrailAppendNode
    {
        LinkedList<Vector3> posLog = new LinkedList<Vector3>();


        #region Unity


        protected override void Start()
        {
            base.Start();
            posLog.AddLast(transform.position);
        }

        #endregion

        protected override bool UpdateInputBuffer()
        {
            var pos = transform.position;
            var posPrev = posLog.Last();

            var update = (Vector3.Distance(posPrev, pos) > gpuTrail.minNodeDistance);
            if (update)
            {
                gpuTrail.inputBuffer_Pos.SetData(new[] { pos });

                posLog.AddLast(pos);

                // _posLogには過去２つの位置を保存しとく
                for (var i = 0; i < posLog.Count - 2; ++i)
                {
                    posLog.RemoveFirst();
                }
            }

            return update;
        }



        #region Debug

        public bool debugDrawLogPoint;

        public override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            if (debugDrawLogPoint)
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