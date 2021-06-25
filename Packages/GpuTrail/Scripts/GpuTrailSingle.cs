using UnityEngine;


namespace GpuTrailSystem
{
    public class GpuTrailSingle : GpuTrailAppendNode
    {
        Vector3 lastPos;

        protected override bool UpdateInputBuffer()
        {
            var pos = transform.position;

            var update = Vector3.Distance(lastPos, pos) > gpuTrail.minNodeDistance;
            if (update)
            {
                lastPos = pos;
                gpuTrail.inputBuffer_Pos.SetData(new[] { pos });
            }

            return update;
        }
    }
}