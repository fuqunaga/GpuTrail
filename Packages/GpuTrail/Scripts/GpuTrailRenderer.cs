using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;


namespace GpuTrailSystem
{
    [RequireComponent(typeof(IGpuTrailHolder))]
    public class GpuTrailRenderer : MonoBehaviour
    {
        #region static

        public static class CSParam
        {
            public static readonly string Kernel_UpdateVertex = "UpdateVertex";

            public static readonly int Time = Shader.PropertyToID("_Time");
            public static readonly int ToCameraDir = Shader.PropertyToID("_ToCameraDir");
            public static readonly int CameraPos = Shader.PropertyToID("_CameraPos");
            public static readonly int StartWidth = Shader.PropertyToID("_StartWidth");
            public static readonly int EndWidth = Shader.PropertyToID("_EndWidth");
            public static readonly int VertexBuffer = Shader.PropertyToID("_VertexBuffer");
        }

        public static class ShaderParam
        {
            public static readonly int VertexNumPerTrail = Shader.PropertyToID("_VertexNumPerTrail");
            public static readonly int IndexBuffer = Shader.PropertyToID("_IndexBuffer");
            public static readonly int VertexBuffer = Shader.PropertyToID("_VertexBuffer");

            public static readonly int TrailIndexBuffer = Shader.PropertyToID("_TrailIndexBuffer");
        }

        #endregion


        public ComputeShader updateVertexCS;
        public Material _material;
        public float _startWidth = 1f;
        public float _endWidth = 1f;

        protected IGpuTrailCulling gpuTrailCulling;

        protected GraphicsBuffer _vertexBuffer;
        protected GraphicsBuffer _indexBuffer;
        protected GraphicsBuffer argsBuffer;

        protected Camera currentCamera;

        IGpuTrailHolder gpuTrailHolder;
        GpuTrail gpuTrail => gpuTrailHolder.GpuTrail;

        public int vertexBufferSize => gpuTrail.nodeBufferSize * 2;

        public int vertexNumPerTrail => gpuTrail.nodeNumPerTrail * 2;
        public int indexNumPerTrail => (gpuTrail.nodeNumPerTrail - 1) * 6;


        [Header("Debug")]
        public bool cullingEnable = true;
        public bool updateVertexEnable = true;
        public bool renderingEnable = true;


        #region Unity

        protected virtual void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        protected virtual void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        protected virtual void Start()
        {
            if (gpuTrailHolder == null)
            {
                gpuTrailHolder = GetComponent<IGpuTrailHolder>();
            }

            if (gpuTrailCulling == null)
            {
                gpuTrailCulling = GetComponent<IGpuTrailCulling>();
            }
        }

        protected virtual void LateUpdate()
        {
            if (updateVertexEnable)
            {
                UpdateVertexBuffer();
            }
        }

        protected virtual void OnRenderObject()
        {
            if (Camera.current != null)
            {
                currentCamera = Camera.current;
            }

            if ((currentCamera == null) || (currentCamera.cullingMask & (1 << gameObject.layer)) == 0)
            {
                return;
            }

            if (renderingEnable)
            {
                OnRenderObjectInternal();
            }
        }


        public virtual void OnDestroy()
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
            var buffers = new[] { _vertexBuffer, _indexBuffer, argsBuffer }.Where(buf => buf != null);
            foreach (var buffer in buffers)
            {
                buffer.Release();
            }
        }


        protected virtual Camera TargetCamera => Camera.main;
        protected virtual bool isCameraOrthographic => TargetCamera.orthographic;
        protected virtual Vector3 toOrthographicCameraDir => -TargetCamera.transform.forward;
        protected virtual Vector3 cameraPos => TargetCamera.transform.position;

        protected bool CullingEnable => cullingEnable && gpuTrailCulling != null;


        void UpdateVertexBuffer()
        {
            if (_vertexBuffer == null) InitBuffer();

            if (CullingEnable)
            {
                gpuTrailCulling.CheckCulling(TargetCamera, gpuTrail, Mathf.Max(_startWidth, _endWidth));
            }

            var cs = updateVertexCS;
            cs.SetFloat(CSParam.Time, Time.time);

            cs.SetVector(CSParam.ToCameraDir, isCameraOrthographic ? toOrthographicCameraDir : Vector3.zero);
            cs.SetVector(CSParam.CameraPos, cameraPos);

            cs.SetFloat(CSParam.StartWidth, _startWidth);
            cs.SetFloat(CSParam.EndWidth, _endWidth);

            var kernel = cs.FindKernel(CSParam.Kernel_UpdateVertex);
            gpuTrail.SetCSParams(cs, kernel);
            if (CullingEnable)
            {
                gpuTrailCulling.SetComputeShaderParameterEnable(cs, kernel);
            }
            else
            {
                if (gpuTrailCulling != null) gpuTrailCulling.SetComputeShaderParameterDisable(cs);
            }
            cs.SetBuffer(kernel, CSParam.VertexBuffer, _vertexBuffer);

            //ComputeShaderUtility.Dispatch(cs, kernel, gpuTrail.nodeBuffer.count);
            ComputeShaderUtility.Dispatch(cs, kernel, gpuTrail.trailNum);


#if false
            var nodes = new Node[gpuTrail.nodeBuffer.count];
            gpuTrail.nodeBuffer.GetData(nodes);
            //nodes = nodes.Take(100).ToArray();
            nodes = nodes.ToArray();

            var vtxs = new Vertex[_vertexBuffer.count];
            _vertexBuffer.GetData(vtxs);
            //vtxs = vtxs.Take(100).ToArray();
            vtxs = vtxs.ToArray();
            for (var i = 0; i < vtxs.Length; ++i)
            {
                if (vtxs[i].pos == Vector3.zero)
                {
                    Debug.Log(i);
                }
            }
#endif
        }



        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            currentCamera = camera;
        }



        protected virtual void SetMaterialParam() { }

        protected virtual void SetCommonMaterialParam()
        {
            SetMaterialParam();
            _material.SetInt(ShaderParam.VertexNumPerTrail, vertexNumPerTrail);
            _material.SetBuffer(ShaderParam.IndexBuffer, _indexBuffer);
            _material.SetBuffer(ShaderParam.VertexBuffer, _vertexBuffer);
        }

        protected virtual void OnRenderObjectInternal()
        {
            SetCommonMaterialParam();
            _material.SetPass(0);

            if (CullingEnable)
            {
                if (argsBuffer == null)
                {
                    argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 4, sizeof(uint));
                    argsBuffer.SetData(new[] { indexNumPerTrail, gpuTrail.trailNum, 0, 0 }); // int[4]{ indexNumPerTrail, trailNum, 0, 0}
                }

                gpuTrailCulling.SetMaterialParameterEnable(_material);
                GraphicsBuffer.CopyCount(gpuTrailCulling.TrailIndexBuffer, argsBuffer, 4);

                Graphics.DrawProceduralIndirectNow(MeshTopology.Triangles, argsBuffer);
            }
            else
            {
                if (gpuTrailCulling != null)
                {
                    gpuTrailCulling.SetMaterialParameterDisable(_material);
                }

                Graphics.DrawProceduralNow(MeshTopology.Triangles, _indexBuffer.count, gpuTrail.trailNum);
            }
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