using UnityEngine;


namespace GpuTrailSystem
{
    /// <summary>
    /// Points that make up a Trail. Vertices for display are generated from Node. 
    /// </summary>
    public struct Node
    {
        public Vector3 pos;
        public float time;
        public Color color;
    }
}