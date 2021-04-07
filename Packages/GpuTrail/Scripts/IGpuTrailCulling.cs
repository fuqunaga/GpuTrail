using UnityEngine;

namespace GpuTrailSystem
{
    public interface IGpuTrailCulling
    {
        public void UpdateTrailIndexBuffer(Camera camera, GpuTrail gpuTrail, float trailWidth, Vector3? cameraPosLocalOffset = null);

        public void SetComputeShaderParameterEnable(ComputeShader cs, int kernel);
        public void SetComputeShaderParameterDisable(ComputeShader cs);

        public GraphicsBuffer TrailIndexBuffer { get; }
    }
}