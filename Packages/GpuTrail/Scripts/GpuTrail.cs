using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;


namespace GpuTrailSystem
{
    [Serializable]
    public class GpuTrail : IDisposable
    {
        public static class CSParam
        {
            public static readonly int TrailNum = Shader.PropertyToID("_TrailNum");
            public static readonly int NodeNumPerTrail = Shader.PropertyToID("_NodeNumPerTrail");
            public static readonly int Life = Shader.PropertyToID("_Life");
            public static readonly int MinNodeDistance = Shader.PropertyToID("_MinNodeDistance");

            public static readonly int TrailBuffer = Shader.PropertyToID("_TrailBuffer");
            public static readonly int NodeBuffer = Shader.PropertyToID("_NodeBuffer");
        }


        public int trailNum = 1;
        public float life = 10f;
        public float inputPerSec = 60f;
        public float minNodeDistance = 0.1f;



        public int nodeNumPerTrail { get; protected set; }
        public int nodeBufferSize => trailNum * nodeNumPerTrail;

        public GraphicsBuffer trailBuffer { get; protected set; }

        public GraphicsBuffer nodeBuffer { get; protected set; }


        public void Init()
        {
            nodeNumPerTrail = Mathf.CeilToInt(life * inputPerSec);
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
            trailBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, trailNum, Marshal.SizeOf<Trail>());
            trailBuffer.SetData(Enumerable.Repeat(default(Trail), trailNum).ToArray());

            nodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodeBufferSize, Marshal.SizeOf<Node>());
            nodeBuffer.SetData(Enumerable.Repeat(default(Node), nodeBufferSize).ToArray());
        }



        protected virtual void ReleaseBuffer()
        {
            if (trailBuffer != null) trailBuffer.Release();
            if (nodeBuffer != null) nodeBuffer.Release();
        }


        public void SetCSParams(ComputeShader cs, int kernel)
        {
            if (trailBuffer == null || nodeBuffer == null)
            {
                Init();
            }

            cs.SetInt(CSParam.TrailNum, trailNum);
            cs.SetInt(CSParam.NodeNumPerTrail, nodeNumPerTrail);
            cs.SetFloat(CSParam.MinNodeDistance, minNodeDistance);
            cs.SetFloat("_Time", Time.time);
            cs.SetFloat(CSParam.Life, life);
            

            cs.SetBuffer(kernel, CSParam.TrailBuffer, trailBuffer);
            cs.SetBuffer(kernel, CSParam.NodeBuffer, nodeBuffer);
        }
    }
}