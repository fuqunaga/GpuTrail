using System.Linq;
using UnityEngine;

namespace GpuTrailSystem
{
    public class GpuTrailRendererCulling
    {
        public static class CsParam
        {
            public const string KernelUpdateTrailIdxBuffer = "UpdateTrailIdxBuffer";
            public static readonly int CameraFrustumNormals = Shader.PropertyToID("_CameraFrustumNormals");
            public static readonly int TrailWidth = Shader.PropertyToID("_TrailWidth");
            public static readonly int CameraPos = Shader.PropertyToID("_CameraPos");
            public static readonly int TrailIndexBufferAppend = Shader.PropertyToID("_TrailIndexBufferAppend");
        }


        protected readonly ComputeShader cullingCs;
        protected GraphicsBuffer trailIndexBuffer;

        public bool debugCameraPosLocalOffsetEnable = default;
        public Vector3 debugCameraPosLocalOffset = default;


        public GpuTrailRendererCulling(ComputeShader cullingCs) => this.cullingCs = cullingCs;

        public void Dispose()
        {
            ReleaseBuffer();
        }


        void InitBuffer(int trailNum)
        {
            trailIndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Append, trailNum, sizeof(uint));
        }

        void ReleaseBuffer()
        {
            trailIndexBuffer?.Release();
        }

        public GraphicsBuffer CalcTrailIndexBuffer(Camera camera, GpuTrail gpuTrail, float trailWidth)
        {
            if (trailIndexBuffer == null)
            {
                InitBuffer(gpuTrail.trailNum);
            }

            var cameraTrans = camera.transform;
            var cameraPos = cameraTrans.position;
            if (debugCameraPosLocalOffsetEnable)
            {
                cameraPos += cameraTrans.rotation * debugCameraPosLocalOffset;
            }

            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var normals = planes.Take(4).Select(p => p.normal).ToList();
            var normalsFloat = Enumerable.Range(0, 3).SelectMany(i => normals.Select(n => n[i])).ToArray(); // row major -> column major


            trailIndexBuffer.SetCounterValue(0);

            var kernel = cullingCs.FindKernel(CsParam.KernelUpdateTrailIdxBuffer);
            gpuTrail.SetCSParams(cullingCs, kernel);
            cullingCs.SetFloat(CsParam.TrailWidth, trailWidth);
            cullingCs.SetFloats(CsParam.CameraFrustumNormals, normalsFloat);
            cullingCs.SetVector(CsParam.CameraPos, cameraPos);
            cullingCs.SetBuffer(kernel, CsParam.TrailIndexBufferAppend, trailIndexBuffer);

            ComputeShaderUtility.Dispatch(cullingCs, kernel, gpuTrail.trailNum);

            return trailIndexBuffer;

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
    }
}