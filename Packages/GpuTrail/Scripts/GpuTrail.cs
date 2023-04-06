using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;


namespace GpuTrailSystem
{
    /// <summary>
    /// C# side corresponding to GpuTrailCSInclude.cginc
    /// </summary>
    [Serializable]
    public class GpuTrail : IDisposable
    {
        public static class CsParam
        {
            public static readonly int TrailNum = Shader.PropertyToID("_TrailNum");
            public static readonly int NodeNumPerTrail = Shader.PropertyToID("_NodeNumPerTrail");
            public static readonly int Time = Shader.PropertyToID("_Time");
            public static readonly int Life = Shader.PropertyToID("_Life");
            public static readonly int MinNodeDistance = Shader.PropertyToID("_MinNodeDistance");

            public static readonly int TrailBuffer = Shader.PropertyToID("_TrailBuffer");
            public static readonly int NodeBuffer = Shader.PropertyToID("_NodeBuffer");
        }


        public int trailNum = 1;
        public float life = 10f;
        public float inputPerSec = 60f;
        public float minNodeDistance = 0.1f;


        public int NodeNumPerTrail { get; protected set; }
        public int NodeNumTotal => trailNum * NodeNumPerTrail;

        public GraphicsBuffer TrailBuffer { get; protected set; }

        public GraphicsBuffer NodeBuffer { get; protected set; }

        public bool IsInitialized => TrailBuffer != null;


        public void Init()
        {
            NodeNumPerTrail = Mathf.CeilToInt(life * inputPerSec);
            if (inputPerSec < Application.targetFrameRate)
            {
                Debug.LogWarning($"inputPerSec({inputPerSec}) < targetFps({Application.targetFrameRate}): Trai adds a node every frame, so running at TargetFrameRate will overflow the buffer.");
            }

            InitBuffer();
        }


        public void Dispose()
        {
            ReleaseBuffer();
        }



        protected virtual void InitBuffer()
        {
            TrailBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, trailNum, Marshal.SizeOf<Trail>());
            TrailBuffer.Fill(default(Trail));

            NodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, NodeNumTotal, Marshal.SizeOf<Node>());
            NodeBuffer.Fill(default(Node));
        }



        protected virtual void ReleaseBuffer()
        {
            TrailBuffer?.Release();
            NodeBuffer?.Release();
        }


        public void SetCSParams(ComputeShader cs, int kernel)
        {
            cs.SetInt(CsParam.TrailNum, trailNum);
            cs.SetInt(CsParam.NodeNumPerTrail, NodeNumPerTrail);
            cs.SetFloat(CsParam.MinNodeDistance, minNodeDistance);
            cs.SetFloat(CsParam.Time, Time.time);
            cs.SetFloat(CsParam.Life, life);

            cs.SetBuffer(kernel, CsParam.TrailBuffer, TrailBuffer);
            cs.SetBuffer(kernel, CsParam.NodeBuffer, NodeBuffer);
        }


        #region Debug

        public void DrawGizmosNodePos(float radius)
        {
            if (NodeBuffer != null && TrailBuffer != null)
            {
                var datas = new Node[NodeBuffer.count];
                NodeBuffer.GetData(datas);

                var trails = new Trail[TrailBuffer.count];
                TrailBuffer.GetData(trails);

                for (var trailIdx = 0; trailIdx < trailNum; ++trailIdx)
                {
                    var totalInputNum = trails[trailIdx].totalInputNum;
                    for (var i = 0; i < totalInputNum; ++i)
                    {
                        Gizmos.DrawWireSphere(datas[NodeNumPerTrail * trailIdx + i].pos, radius);
                    }
                }
            }
        }

        #endregion
    }
}