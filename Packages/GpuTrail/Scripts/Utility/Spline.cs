using UnityEngine;


namespace GpuTrailSystem
{
    public static class Spline
    {
        /// <summary>
        /// start～end をつなぐ曲線上の点をtに応じて求める
        /// http://t-pot.com/program/2_3rdcurve/index.html
        /// </summary>
        /// <param name="t">0f~1f</param>
        /// <returns></returns>
        public static Vector3 CatmullRom(float t, Vector3 prev, Vector3 start, Vector3 end)
        {
            var t2 = t * t;

            return
                0.5f * (
                  t2 * (prev - 2f * start + end)
                + t * (-prev + end)
                )
                + start;
        }
    }
}