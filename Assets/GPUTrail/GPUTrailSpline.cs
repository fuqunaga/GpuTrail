using UnityEngine;
using System.Collections;

public class GPUTrailSpline : MonoBehaviour
{
    /// <summary>
    /// start～end をつなぐ曲線上の点をtに応じて求める
    /// http://t-pot.com/program/2_3rdcurve/index.html
   /// </summary>
    /// <param name="t">0f~1f</param>
    /// <param name="prev"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public static Vector3 CatmullRom(float t, Vector3 prev, Vector3 start, Vector3 end)
    {
        var t2 = t * t;

        return 
            0.5f * ( 
              t2 * (prev - 2f * start + end) 
            + t  * (-prev + end)
            )
            + start;
    }
}
