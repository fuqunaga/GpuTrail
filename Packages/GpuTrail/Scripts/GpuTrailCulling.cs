using System.Linq;
using UnityEngine;


namespace GpuTrailSystem
{
    public interface IGpuTrailCulling
    {

        public void UpdateTrailIndexBuffer(Camera camera, GpuTrail gpuTrail, float trailWidth);

        public void SetComputeShaderParameterEnable(ComputeShader cs, int kernel);
        public void SetComputeShaderParameterDisable(ComputeShader cs);

        public GraphicsBuffer TrailIndexBuffer { get; }
    }

    public class GpuTrailCulling : MonoBehaviour, IGpuTrailCulling
    {
        public static class ShaderParam
        {
            // for ComputeShader
            public static readonly string Kernel_UpdateTrailIdxBuffer = "UpdateTrailIdxBuffer";
            public static readonly int CameraFrustumNormals = Shader.PropertyToID("_CameraFrustumNormals");
            public static readonly int TrailWidth = Shader.PropertyToID("_TrailWidth");
            public static readonly int CameraPos = Shader.PropertyToID("_CameraPos");

            // for ComputeShader and Shader
            public static readonly string Keyword_TrailIdxOn = "GPUTRAIL_TRAIL_INDEX_ON";
            public static readonly int TrailIndexBuffer = Shader.PropertyToID("_TrailIndexBuffer");
        }


        public ComputeShader cullingCS;

        protected GraphicsBuffer trailIndexBuffer;

        public GraphicsBuffer TrailIndexBuffer => trailIndexBuffer;

        void OnDestroy()
        {
            ReleaseBuffer();
        }


        public void InitBuffer(int trailNum)
        {
            trailIndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Append, trailNum, sizeof(uint));
        }

        public void ReleaseBuffer()
        {
            if (trailIndexBuffer != null) trailIndexBuffer.Release();
        }

        public void UpdateTrailIndexBuffer(Camera camera, GpuTrail gpuTrail, float trailWidth)
        {
            if (trailIndexBuffer == null)
            {
                InitBuffer(gpuTrail.trailNum);
            }

            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var normals = planes.Take(4).Select(p => p.normal).ToList();
            //planes.Take(4).ToList().ForEach(plane => Debug.DrawRay(camera.transform.position, plane.normal * 10f));
            var normalsFloat = Enumerable.Range(0, 3).SelectMany(i => normals.Select(n => n[i])).ToArray(); // row major -> column major
            cullingCS.SetFloats(ShaderParam.CameraFrustumNormals, normalsFloat);

            trailIndexBuffer.SetCounterValue(0);

            var kernel = cullingCS.FindKernel(ShaderParam.Kernel_UpdateTrailIdxBuffer);
            gpuTrail.SetCSParams(cullingCS, kernel);
            cullingCS.SetFloat(ShaderParam.TrailWidth, trailWidth);
            cullingCS.SetVector(ShaderParam.CameraPos, camera.transform.position);
            //cullingCS.SetBuffer(kernel, "_IsInView", _trailIsInViews);
            cullingCS.SetBuffer(kernel, ShaderParam.TrailIndexBuffer, trailIndexBuffer);

            ComputeShaderUtility.Dispatch(cullingCS, kernel, gpuTrail.trailNum);


#if true
        }
#else
            if (tmpBuf == null)
            {
                tmpBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, sizeof(uint));
            }
            GraphicsBuffer.CopyCount(trailIndexBuffer, tmpBuf, 0);
            var count = new uint[1];
            tmpBuf.GetData(count);
            Debug.Log(count.First());
            var trailIdx = new uint[trailIndexBuffer.count];
            trailIndexBuffer.GetData(trailIdx);
        }
        GraphicsBuffer tmpBuf;
#endif

        public void SetComputeShaderParameterEnable(ComputeShader cs, int kernel)
        {
            cs.EnableKeyword(ShaderParam.Keyword_TrailIdxOn);
            cs.SetBuffer(kernel, ShaderParam.TrailIndexBuffer, trailIndexBuffer);
        }

        public void SetComputeShaderParameterDisable(ComputeShader cs)
        {
            cs.DisableKeyword(ShaderParam.Keyword_TrailIdxOn);
        }
    }
}