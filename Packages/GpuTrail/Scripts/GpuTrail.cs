using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;


namespace GpuTrailSystem
{
    public abstract class GpuTrail : MonoBehaviour
    {
        public static class ShaderParam
        {
            public static readonly int TrailNum = Shader.PropertyToID("_TrailNum");
            public static readonly int NodeNumPerTrail = Shader.PropertyToID("_NodeNumPerTrail");
            public static readonly int InputNodeNum = Shader.PropertyToID("_InputNodeNum");
            public static readonly int MinNodeDistance = Shader.PropertyToID("_MinNodeDistance");
            public static readonly int Time = Shader.PropertyToID("_Time");
            public static readonly int Life = Shader.PropertyToID("_Life");
            public static readonly int ToCameraDir = Shader.PropertyToID("_ToCameraDir");
            public static readonly int CameraPos = Shader.PropertyToID("_CameraPos");
        }


        public ComputeShader _cs;
        public float _life = 10f;
        public float _inputPerSec = 60f;
        public int _inputNumMax = 5;
        public float _minNodeDistance = 0.1f;

        public int nodeNumPerTrail { get; protected set; }

        public GraphicsBuffer nodeBuffer { get; protected set; }
        //protected GraphicsBuffer _vertexBuffer;

        protected Camera currentCamera;


        public abstract int trailNumMax { get; }
        public int nodeBufferSize => trailNumMax * nodeNumPerTrail;
        public int vertexBufferSize => nodeBufferSize * 2;

        public int vertexNumPerTrail => nodeNumPerTrail * 2;
        public int indexNumPerTrail => (nodeNumPerTrail - 1) * 6;


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

            nodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodeBufferSize, Marshal.SizeOf(typeof(Node)));
            nodeBuffer.SetData(Enumerable.Repeat(default(Node), nodeBuffer.count).ToArray());
        }



        protected virtual void ReleaseBuffer()
        {
            if (nodeBuffer != null)
            {
                nodeBuffer.Release();
            }
        }


        protected virtual bool isCameraOrthographic => Camera.main.orthographic;
        protected virtual Vector3 toOrthographicCameraDir => -Camera.main.transform.forward;
        protected virtual Vector3 cameraPos => Camera.main.transform.position;

        protected void SetCommonParameterForCS()
        {
            _SetCommonParameterForCS(_cs);
        }

        protected void _SetCommonParameterForCS(ComputeShader cs)
        {
            cs.SetInt(ShaderParam.TrailNum, trailNumMax);
            cs.SetInt(ShaderParam.NodeNumPerTrail, nodeNumPerTrail);

            cs.SetInt(ShaderParam.InputNodeNum, Mathf.Min(_inputNumMax, Mathf.FloorToInt(_inputNumCurrent)));
            cs.SetFloat(ShaderParam.MinNodeDistance, _minNodeDistance);
            cs.SetFloat (ShaderParam.Time, Time.time);
            cs.SetFloat (ShaderParam.Life, _life);

            cs.SetVector(ShaderParam.ToCameraDir, isCameraOrthographic ? toOrthographicCameraDir : Vector3.zero);
            cs.SetVector(ShaderParam.CameraPos, cameraPos);
        }

        float _inputNumCurrent;
        protected virtual void LateUpdate()
        {
            _inputNumCurrent = Time.deltaTime * _inputPerSec + (_inputNumCurrent - Mathf.Floor(_inputNumCurrent)); // continue under dicimal
            UpdateVertex();
        }

        protected abstract void UpdateVertex();
    }
}