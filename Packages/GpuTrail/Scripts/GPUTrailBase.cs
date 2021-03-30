using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace GpuTrailSystem
{
    public abstract class GpuTrailBase : MonoBehaviour
    {
        #region TypeDefine
        public enum LerpType
        {
            None,
            Spline
        }


        public struct Node
        {
            public Vector3 pos;
            public float time;
            public Color color;
        }

        public struct Vertex
        {
            public Vector3 pos;
            public Vector2 uv;
            public Color color;
        }

        #endregion


        public ComputeShader _cs;
        public Material _material;
        public float _life = 10f;
        public float _inputPerSec = 60f;
        public int _inputNumMax = 5;
        public LerpType _lerpType = LerpType.Spline;
        public float _minNodeDistance = 0.1f;
        public float _startWidth = 1f;
        public float _endWidth = 1f;

        protected int _nodeNumPerTrail;

        protected ComputeBuffer _nodeBuffer;
        protected ComputeBuffer _vertexBuffer;
        protected ComputeBuffer _indexBuffer;

        protected Camera currentCamera;


        protected abstract int trailNumMax { get; }
        public int nodeBufferSize { get { return trailNumMax * _nodeNumPerTrail; } }
        public int vertexBufferSize { get { return nodeBufferSize * 2; } }

        public int vertexNumPerTrail { get { return _nodeNumPerTrail * 2; } }
        public int indexNumPerTrail { get { return (_nodeNumPerTrail - 1) * 6; } }


        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        protected virtual void Awake()
        {
            ReleaseBuffer();

            _nodeNumPerTrail = Mathf.CeilToInt(_life * _inputPerSec);
            if (_inputPerSec < Application.targetFrameRate)
            {
                Debug.LogWarning($"inputPerSec({_inputPerSec}) < targetFps({Application.targetFrameRate}): Trai adds a node every frame, so running at TargetFrameRate will overflow the buffer.");
            }

            InitBuffer();
        }


        protected virtual void InitBuffer()
        {
            _nodeBuffer = new ComputeBuffer(nodeBufferSize, Marshal.SizeOf(typeof(Node)));
            _nodeBuffer.SetData(Enumerable.Repeat(default(Node), _nodeBuffer.count).ToArray());

            _vertexBuffer = new ComputeBuffer(vertexBufferSize, Marshal.SizeOf(typeof(Vertex))); // 1 node to 2 vtx(left,right)
            _vertexBuffer.SetData(Enumerable.Repeat(default(Vertex), _vertexBuffer.count).ToArray());

            // 各Nodeの最後と次のNodeの最初はポリゴンを繋がないので-1
            var indexData = new int[indexNumPerTrail];
            var iidx = 0;
            for (var iNode = 0; iNode < _nodeNumPerTrail - 1; ++iNode)
            {
                var offset = +iNode * 2;
                indexData[iidx++] = 0 + offset;
                indexData[iidx++] = 1 + offset;
                indexData[iidx++] = 2 + offset;
                indexData[iidx++] = 2 + offset;
                indexData[iidx++] = 1 + offset;
                indexData[iidx++] = 3 + offset;
            }

            _indexBuffer = new ComputeBuffer(indexData.Length, Marshal.SizeOf(typeof(uint))); // 1 node to 2 triangles(6vertexs)
            _indexBuffer.SetData(indexData);
        }



        protected virtual void ReleaseBuffer()
        {
            new[] { _nodeBuffer, _vertexBuffer, _indexBuffer }
                .Where(b => b != null)
                .ToList().ForEach(buffer =>
                {
                    buffer.Release();
                });
        }


        protected virtual bool isCameraOrthographic { get { return Camera.main.orthographic; } }
        protected virtual Vector3 toOrthographicCameraDir { get { return -Camera.main.transform.forward; } }
        protected virtual Vector3 cameraPos { get { return Camera.main.transform.position; } }

        protected void SetCommonParameterForCS()
        {
            _SetCommonParameterForCS(_cs);
        }
        protected void _SetCommonParameterForCS(ComputeShader cs)
        {
            cs.SetInt("_TrailNum", trailNumMax);
            cs.SetInt("_NodeNumPerTrail", _nodeNumPerTrail);

            cs.SetInt("_InputNodeNum", Mathf.Min(_inputNumMax, Mathf.FloorToInt(_inputNumCurrent)));
            //cs.SetInt("_LerpType", (int)_lerpType);
            cs.SetFloat("_MinNodeDistance", _minNodeDistance);
            cs.SetFloat("_Time", Time.time);
            cs.SetFloat("_Life", _life);

            cs.SetVector("_ToCameraDir", isCameraOrthographic ? toOrthographicCameraDir : Vector3.zero);
            cs.SetVector("_CameraPos", cameraPos);
            cs.SetFloat("_StartWidth", _startWidth);
            cs.SetFloat("_EndWidth", _endWidth);
        }

        float _inputNumCurrent;
        protected virtual void LateUpdate()
        {
            _inputNumCurrent = Time.deltaTime * _inputPerSec + (_inputNumCurrent - Mathf.Floor(_inputNumCurrent)); // continue under dicimal
            UpdateVertex();
        }

        protected abstract void UpdateVertex();

        void OnRenderObject()
        {
            if (Camera.current != null)
            {
                currentCamera = Camera.current;
            }

            if ((currentCamera == null) || (currentCamera.cullingMask & (1 << gameObject.layer)) == 0)
            {
                return;
            }

            OnRenderObjectInternal();
        }


        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            currentCamera = camera;
        }



        protected virtual void SetMaterialParam() { }
        protected virtual void SetCommonMaterialParam()
        {
            SetMaterialParam();
            _material.SetInt("_VertexNumPerTrail", vertexNumPerTrail);
            _material.SetBuffer("_IndexBuffer", _indexBuffer);
            _material.SetBuffer("_VertexBuffer", _vertexBuffer);
        }

        protected virtual void OnRenderObjectInternal()
        {
            SetCommonMaterialParam();

            _material.DisableKeyword("GPUTRAIL_TRAIL_INDEX_ON");
            _material.SetPass(0);

            Graphics.DrawProceduralNow(MeshTopology.Triangles, _indexBuffer.count, trailNumMax);
        }

        public void OnDestroy()
        {
            ReleaseBuffer();
        }
    }
}