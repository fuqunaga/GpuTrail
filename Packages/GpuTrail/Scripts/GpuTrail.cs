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
        public static class CSParam
        {
            public static readonly string Kernel_AppendNode = "AppendNode";
            public static readonly string Keyword_IgnoreOrigin = "IGNORE_ORIGIN";

            public static readonly int TrailNum = Shader.PropertyToID("_TrailNum");
            public static readonly int NodeNumPerTrail = Shader.PropertyToID("_NodeNumPerTrail");
            public static readonly int Time = Shader.PropertyToID("_Time");
            public static readonly int Life = Shader.PropertyToID("_Life");
            public static readonly int MinNodeDistance = Shader.PropertyToID("_MinNodeDistance");

            public static readonly int TrailBuffer = Shader.PropertyToID("_TrailBuffer");
            public static readonly int NodeBuffer = Shader.PropertyToID("_NodeBuffer");

            public static readonly int InputBuffer_Pos = Shader.PropertyToID("_InputBuffer_Pos");
            public static readonly int InputBuffer_Color = Shader.PropertyToID("_InputBuffer_Color");

        }

        public ComputeShader appendNodeCS;
        public int trailNum = 1;
        public float life = 10f;
        public float inputPerSec = 60f;
        public float minNodeDistance = 0.1f;
        [Tooltip("Ignore (0,0,0) position input")]
        public bool ignoreOriginInput = true;


        public int nodeNumPerTrail { get; protected set; }
        public int nodeNumTotal => trailNum * nodeNumPerTrail;

        public GraphicsBuffer trailBuffer { get; protected set; }

        public GraphicsBuffer nodeBuffer { get; protected set; }


        public GraphicsBuffer inputBuffer_Pos { get; protected set; }

        public GraphicsBuffer inputBuffer_Color { get; protected set; }


        public bool isInitialized => trailBuffer != null;


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

            nodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodeNumTotal, Marshal.SizeOf<Node>());
            nodeBuffer.SetData(Enumerable.Repeat(default(Node), nodeNumTotal).ToArray());


            inputBuffer_Pos = new GraphicsBuffer(GraphicsBuffer.Target.Structured, trailNum, Marshal.SizeOf<Vector3>());
            inputBuffer_Pos.SetData(Enumerable.Repeat(default(Vector3), trailNum).ToArray());

            inputBuffer_Color = new GraphicsBuffer(GraphicsBuffer.Target.Structured, trailNum, Marshal.SizeOf<Color>());
            inputBuffer_Color.SetData(Enumerable.Repeat(Color.gray, trailNum).ToArray());
        }



        protected virtual void ReleaseBuffer()
        {
            if (trailBuffer != null) trailBuffer.Release();
            if (nodeBuffer != null) nodeBuffer.Release();
            if (inputBuffer_Pos != null) inputBuffer_Pos.Release();
            if (inputBuffer_Color != null) inputBuffer_Color.Release();
        }


        public void SetCSParams(ComputeShader cs, int kernel)
        {
            if (trailBuffer == null || nodeBuffer == null)
            {
                Init();
            }

            if ( ignoreOriginInput)
            {
                cs.EnableKeyword(CSParam.Keyword_IgnoreOrigin);
            }
            else
            {
                cs.DisableKeyword(CSParam.Keyword_IgnoreOrigin);
            }

            cs.SetInt(CSParam.TrailNum, trailNum);
            cs.SetInt(CSParam.NodeNumPerTrail, nodeNumPerTrail);
            cs.SetFloat(CSParam.MinNodeDistance, minNodeDistance);
            cs.SetFloat(CSParam.Time, Time.time);
            cs.SetFloat(CSParam.Life, life);
            
            cs.SetBuffer(kernel, CSParam.TrailBuffer, trailBuffer);
            cs.SetBuffer(kernel, CSParam.NodeBuffer, nodeBuffer);

            cs.SetBuffer(kernel, CSParam.InputBuffer_Pos, inputBuffer_Pos);
            cs.SetBuffer(kernel, CSParam.InputBuffer_Color, inputBuffer_Color);
        }

        public void DispatchAppendNode()
        {
            var kernel = appendNodeCS.FindKernel(CSParam.Kernel_AppendNode);
            SetCSParams(appendNodeCS, kernel);

            ComputeShaderUtility.Dispatch(appendNodeCS, kernel, trailNum);

            /*
            var inputPos = new Vector3[inputBuffer_Pos.count];
            inputBuffer_Pos.GetData(inputPos);

            var nodes = new Node[nodeBuffer.count];
            nodeBuffer.GetData(nodes);
            nodes = nodes.Take(100).ToArray();
            */
        }


        #region Debug

        public void DrawGizmosInputPos(float radius)
        {
            var datas = new Vector3[inputBuffer_Pos.count];
            inputBuffer_Pos.GetData(datas);

            for(var i=0; i<datas.Length; ++i)
            {
                Gizmos.DrawWireSphere(datas[i], radius);
            }
        }

        public void DrawGizmosNodePos(float radius)
        {
            var datas = new Node[nodeBuffer.count];
            nodeBuffer.GetData(datas);

            for (var i = 0; i < datas.Length; ++i)
            {
                Gizmos.DrawWireSphere(datas[i].pos, radius);
            }
        }

        #endregion
    }
}