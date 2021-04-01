using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;


namespace GpuTrailSystem
{
    [RequireComponent(typeof(IGpuTrailHolder))]
    public class GpuTrailRenderer : MonoBehaviour
    {
        public static class CSParam
        {
            public static readonly int Time = Shader.PropertyToID("_Time");
            public static readonly int ToCameraDir = Shader.PropertyToID("_ToCameraDir");
            public static readonly int CameraPos = Shader.PropertyToID("_CameraPos");
        }


        public ComputeShader updateVertexCS;
        public Material _material;
        public float _startWidth = 1f;
        public float _endWidth = 1f;

        protected GraphicsBuffer _vertexBuffer;
        protected GraphicsBuffer _indexBuffer;

        protected Camera currentCamera;

        IGpuTrailHolder gpuTrailHolder;
        GpuTrail gpuTrail => gpuTrailHolder.GpuTrail;

        public int vertexBufferSize => gpuTrail.nodeBufferSize * 2;

        public int vertexNumPerTrail => gpuTrail.nodeNumPerTrail * 2;
        public int indexNumPerTrail => (gpuTrail.nodeNumPerTrail - 1) * 6;


        #region Unity

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        void Start()
        {
            if (gpuTrailHolder == null)
            {
                gpuTrailHolder = GetComponent<IGpuTrailHolder>();
            }
        }

        void LateUpdate()
        {
            UpdateVertexBuffer();
        }

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


        public void OnDestroy()
        {
            ReleaseBuffers();
        }

        #endregion


        protected virtual void InitBuffer()
        {
            _vertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vertexBufferSize, Marshal.SizeOf<Vertex>()); // 1 node to 2 vtx(left,right)
            _vertexBuffer.SetData(Enumerable.Repeat(default(Vertex), _vertexBuffer.count).ToArray());

            // 各Nodeの最後と次のNodeの最初はポリゴンを繋がないので-1
            var indexData = new int[indexNumPerTrail];
            var iidx = 0;
            for (var iNode = 0; iNode < gpuTrail.nodeNumPerTrail - 1; ++iNode)
            {
                var offset = +iNode * 2;
                indexData[iidx++] = 0 + offset;
                indexData[iidx++] = 1 + offset;
                indexData[iidx++] = 2 + offset;
                indexData[iidx++] = 2 + offset;
                indexData[iidx++] = 1 + offset;
                indexData[iidx++] = 3 + offset;
            }

            _indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, indexData.Length, Marshal.SizeOf<uint>()); // 1 node to 2 triangles(6vertexs)
            _indexBuffer.SetData(indexData);
        }



        protected virtual void ReleaseBuffers()
        {
            var buffers = new[] { _vertexBuffer, _indexBuffer }.Where(buf => buf != null);
            foreach (var buffer in buffers)
            {
                buffer.Release();
            }
        }

        protected virtual bool isCameraOrthographic => Camera.main.orthographic;
        protected virtual Vector3 toOrthographicCameraDir => -Camera.main.transform.forward;
        protected virtual Vector3 cameraPos => Camera.main.transform.position;


 
        void UpdateVertexBuffer()
        {
            if (_vertexBuffer == null) InitBuffer();

            var cs = updateVertexCS;
            

            cs.SetFloat(CSParam.Time, Time.time);

            cs.SetVector(CSParam.ToCameraDir, isCameraOrthographic ? toOrthographicCameraDir : Vector3.zero);
            cs.SetVector(CSParam.CameraPos, cameraPos);

            cs.SetFloat("_StartWidth", _startWidth);
            cs.SetFloat("_EndWidth", _endWidth);

            var kernel = cs.FindKernel("UpdateVertex");
            gpuTrail.SetCSParams(cs, kernel);
            cs.SetBuffer(kernel, "_VertexBuffer", _vertexBuffer);

            ComputeShaderUtility.Dispatch(cs, kernel, gpuTrail.nodeBuffer.count);

            
            /*
            var nodes = new Node[gpuTrail.nodeBuffer.count];
            gpuTrail.nodeBuffer.GetData(nodes);

            var vtxs = new Vertex[_vertexBuffer.count];
            _vertexBuffer.GetData(vtxs);
            */
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

            Graphics.DrawProceduralNow(MeshTopology.Triangles, _indexBuffer.count, gpuTrail.trailNum);
        }



        #region Debug

        public bool _debugDrawVertexBuf;

        public void OnDrawGizmosSelected()
        {
            if (_debugDrawVertexBuf)
            {
                Gizmos.color = Color.yellow;
                var data = new Vertex[_vertexBuffer.count];
                _vertexBuffer.GetData(data);

                var num = _vertexBuffer.count / 2;
                for (var i = 0; i < num; ++i)
                {
                    var v0 = data[2 * i];
                    var v1 = data[2 * i + 1];

                    Gizmos.DrawLine(v0.pos, v1.pos);
                }
            }
        }

        #endregion
    }
}