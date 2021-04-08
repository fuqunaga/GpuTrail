using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GpuTrailSystem
{
    [RequireComponent(typeof(IGpuTrailHolder))]
    public class GpuTrailRenderer : MonoBehaviour
    {
        public ComputeShader computeShader;
        public Material material;
        public float startWidth = 1f;
        public float endWidth = 1f;

        protected IGpuTrailCulling gpuTrailCulling;
        protected Camera currentCamera;

        IGpuTrailHolder gpuTrailHolder;
        GpuTrail gpuTrail => gpuTrailHolder.GpuTrail;

        [SerializeField] // for debug
        protected List<GpuTrailRenderer_Lod> lodList = new List<GpuTrailRenderer_Lod>();

        [Header("Debug")]
        public bool cullingEnable = true;
        public Vector3 cullingCameraLocalPosOffset;
        public bool updateVertexEnable = true;
        public bool renderingEnable = true;


        protected virtual Camera TargetCamera => Camera.main;


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
                if ( gpuTrailCulling == null)
                {
                    GpuTrailCulling.SetComputeShaderParameterDisableDefault(computeShader);
                }
            }

            lodList.Add(new GpuTrailRenderer_Lod(gpuTrail, computeShader));
        }

        protected virtual void LateUpdate()
        {
            if (gpuTrailCulling != null)
            {
                if (cullingEnable)
                {
                    float width = Mathf.Max(startWidth, endWidth);
                    gpuTrailCulling.UpdateTrailIndexBuffer(TargetCamera, gpuTrail, width, cullingCameraLocalPosOffset);

                    lodList.ForEach(lod => lod.UpdateArgsBuffer(gpuTrailCulling));
                }
                else
                {
                    lodList.ForEach(lod => lod.ResetArgsBuffer());
                }
            }


            if (updateVertexEnable)
            {
                lodList.ForEach(lod => lod.UpdateVertexBuffer(gpuTrailCulling, TargetCamera, startWidth, endWidth, cullingEnable));
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
                lodList.ForEach(lod => lod.OnRenderObject(material));
            }
        }


        public virtual void OnDestroy()
        {
            lodList.ForEach(lod => lod.Dispose());
        }

        #endregion


        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            currentCamera = camera;
        }


        #region Debug

        public void OnDrawGizmosSelected()
        {
            lodList.ForEach(lod => lod.OnDrawGizmosSelected());
        }

        #endregion
    }
}