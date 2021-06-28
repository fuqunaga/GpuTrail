using System.Collections.Generic;
using UnityEngine;


namespace GpuTrailSystem
{

    public class SplineExample : MonoBehaviour
    {
        public int step = 10;
        public List<Transform> _transforms = new List<Transform>();


        public void OnDrawGizmos()
        {
            var stepInv = 1f / step;
            for (var i = 2; i < _transforms.Count; ++i)
            {
                for (var s = 0; s < step; ++s)
                {
                    var prev = _transforms[i - 2].position;
                    var start = _transforms[i - 1].position;
                    var end = _transforms[i].position;

                    var pos = Spline.CatmullRom(stepInv * s, prev, start, end);
                    var posNext = Spline.CatmullRom(stepInv * (s + 1), prev, start, end);


                    Gizmos.DrawLine(pos, posNext);
                }
            }
        }
    }
}