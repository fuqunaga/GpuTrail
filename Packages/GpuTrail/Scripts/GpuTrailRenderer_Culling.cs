using System;
using System.Linq;
using UnityEngine;


namespace GpuTrailSystem
{
    [Serializable]
    public class GpuTrailRenderer_Culling
    {
        public static class CSParam
        {
            public static readonly string Kernel_UpdateTrailIdxBuffer = "UpdateTrailIdxBuffer";
            public static readonly int CameraFrustumNormals = Shader.PropertyToID("_CameraFrustumNormals");
            public static readonly int TrailWidth = Shader.PropertyToID("_TrailWidth");
            public static readonly int CameraPos = Shader.PropertyToID("_CameraPos");
            public static readonly int TrailIndexBufferAppend = Shader.PropertyToID("_TrailIndexBufferAppend");
        }


        public ComputeShader cullingCS;
        protected GraphicsBuffer trailIndexBuffer;

        public GpuTrailRenderer_Culling(ComputeShader cullingCS) => this.cullingCS = cullingCS;

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
            if (trailIndexBuffer != null) trailIndexBuffer.Release();
        }

        public virtual GraphicsBuffer CalcTrailIndexBuffer(Camera camera, GpuTrail gpuTrail, float trailWidth/*, Vector3? cameraPosLocalOffset*/)
        {
            if (trailIndexBuffer == null)
            {
                InitBuffer(gpuTrail.trailNum);
            }

            var cameraTrans = camera.transform;
            var cameraPos = cameraTrans.position;
            /*
            if (cameraPosLocalOffset.HasValue)
            {
                cameraPos += cameraTrans.rotation * cameraPosLocalOffset.Value;
            }
            */

            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var normals = planes.Take(4).Select(p => p.normal).ToList();
            var normalsFloat = Enumerable.Range(0, 3).SelectMany(i => normals.Select(n => n[i])).ToArray(); // row major -> column major



            trailIndexBuffer.SetCounterValue(0);

            var kernel = cullingCS.FindKernel(CSParam.Kernel_UpdateTrailIdxBuffer);
            gpuTrail.SetCSParams(cullingCS, kernel);
            cullingCS.SetFloat(CSParam.TrailWidth, trailWidth);
            cullingCS.SetFloats(CSParam.CameraFrustumNormals, normalsFloat);
            cullingCS.SetVector(CSParam.CameraPos, cameraPos);
            cullingCS.SetBuffer(kernel, CSParam.TrailIndexBufferAppend, trailIndexBuffer);

            ComputeShaderUtility.Dispatch(cullingCS, kernel, gpuTrail.trailNum);

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