using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace GpuTrailSystem
{
    [RequireComponent(typeof(IGpuTrailAppendNode))]
    public class GpuTrailRenderer : MonoBehaviour
    {
        [System.Serializable]
        public class LodSetting
        {
            public float distance = 0f;
            public int lodNodeStep = 1;
        }

        public ComputeShader computeShader;
        public Material material;
        public float startWidth = 1f;
        public float endWidth = 1f;

        protected IGpuTrailCulling gpuTrailCulling;
        protected GpuTrailCalcLod gpuTrailCalcLod;
        protected Camera currentCamera;

        IGpuTrailAppendNode gpuTrailAppendNode;
        GpuTrail gpuTrail => gpuTrailAppendNode.GpuTrail;

        [SerializeField]
        protected List<LodSetting> lodSettings = new List<LodSetting>();
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
            if (gpuTrailAppendNode == null)
            {
                gpuTrailAppendNode = GetComponent<IGpuTrailAppendNode>();
            }

            if (gpuTrailCulling == null)
            {
                gpuTrailCulling = GetComponent<IGpuTrailCulling>();
                if ( gpuTrailCulling == null)
                {
                    GpuTrailCulling.SetComputeShaderParameterDisableDefault(computeShader);
                }
            }

            if (gpuTrailCalcLod == null)
            {
                gpuTrailCalcLod = GetComponent<GpuTrailCalcLod>();
            }

            if (!lodSettings.Any()) lodSettings.Add(new LodSetting());
        }

        protected virtual void LateUpdate()
        {
            if (lodSettings.Count != lodList.Count) ResetLodList();


            gpuTrailAppendNode.AppendNode();

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


            if (gpuTrailCalcLod != null)
            {
                gpuTrailCalcLod.CalcLod(TargetCamera, gpuTrail, lodSettings);
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
            DisposeLodList();
        }

        #endregion


        protected void ResetLodList()
        {
            DisposeLodList();

            lodList = lodSettings.Select(settings => new GpuTrailRenderer_Lod(gpuTrail, computeShader, settings)).ToList();
        }

        void DisposeLodList()
        {
            lodList.ForEach(lod => lod.Dispose());
            lodList.Clear();
        }


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