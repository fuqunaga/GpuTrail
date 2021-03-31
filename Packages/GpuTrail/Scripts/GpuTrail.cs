using System.Runtime.InteropServices;
using UnityEngine;


namespace GpuTrailSystem
{
    public abstract class GpuTrail : MonoBehaviour
    {
        public static class CSParam
        {
            public static readonly int TrailNum = Shader.PropertyToID("_TrailNum");
            public static readonly int NodeNumPerTrail = Shader.PropertyToID("_NodeNumPerTrail");
            public static readonly int Life = Shader.PropertyToID("_Life");
        }


        public ComputeShader _cs;
        public float _life = 10f;
        public float _inputPerSec = 60f;
        

        public int nodeNumPerTrail { get; protected set; }

        public GraphicsBuffer nodeBuffer { get; protected set; }


        public abstract int trailNumMax { get; }
        public int nodeBufferSize => trailNumMax * nodeNumPerTrail;


        #region Unity

        protected virtual void Awake()
        {
            nodeNumPerTrail = Mathf.CeilToInt(_life * _inputPerSec);
            if (_inputPerSec < Application.targetFrameRate)
            {
                Debug.LogWarning($"inputPerSec({_inputPerSec}) < targetFps({Application.targetFrameRate}): Trai adds a node every frame, so running at TargetFrameRate will overflow the buffer.");
            }

            InitBuffer();
        }


        public void OnDestroy()
        {
            ReleaseBuffer();
        }

        #endregion



        protected virtual void InitBuffer()
        {
            ReleaseBuffer();

            nodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodeBufferSize, Marshal.SizeOf<Node>());
        }



        protected virtual void ReleaseBuffer()
        {
            if (nodeBuffer != null)
            {
                nodeBuffer.Release();
            }
        }


        public void SetCSParams(ComputeShader cs)
        {
            cs.SetInt(CSParam.TrailNum, trailNumMax);
            cs.SetInt(CSParam.NodeNumPerTrail, nodeNumPerTrail);
            
            cs.SetFloat (CSParam.Life, _life);
        }

        protected virtual void LateUpdate()
        {
            SetCSParams(_cs);
            UpdateNode();
        }

        protected abstract void UpdateNode();
    }
}