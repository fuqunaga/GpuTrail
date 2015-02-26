using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GPUTrail : MonoBehaviour {

    public struct Point
    {
        public Vector3 pos;
        public float time;
    }

    public struct Vertex
    {
        public Vector3 pos;
        public Vector2 uv;
        public Color color;
    }


    public Material material;
    public float time = 1f;
    public float startWidth = 1f;
    public float endWidth = 1f;
    public Color startColor = Color.white;
    public Color endColor = Color.white;
    public float minVertexDistance = 0.1f;
    public ComputeShader cs;


    int bufferSize;
    int totalInputIdx = -1;
    Vector3 lastInputPos;

	float startTime;

    ComputeBuffer inputBuffer;
    ComputeBuffer pointBuffer;
    ComputeBuffer vertexBuffer;

    const float FPS = 60f;
    void Start()
    {
        ReleaseBuffer();

        bufferSize = Mathf.CeilToInt(time * FPS);

        inputBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Point)));
        pointBuffer = new ComputeBuffer(bufferSize, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Point)));

        vertexBuffer = new ComputeBuffer(bufferSize * 6, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vertex))); // 1 input to 2 triangles(6vertexs)

        lastInputPos = transform.position;
		startTime = Time.time;
    }

    void ReleaseBuffer()
    {
        new[] { inputBuffer, pointBuffer, vertexBuffer }
            .Where(b => b != null)
            .ToList().ForEach(buffer =>
            {
                buffer.Release();
                buffer = null;
            });
    }

    void Update()
    {
        var pos = transform.position;

        if ((Vector3.Distance(lastInputPos, pos) > minVertexDistance))
        {
            inputBuffer.SetData(new[]{new Point()
            {
                pos = pos,
                time = Time.time
            }});

            
            totalInputIdx++;
            if (totalInputIdx == 0) startTime = Time.time;
            lastInputPos = pos;
        }

        UpdateVertex();
    }

    const int NUM_THREAD_X = 16;
    void UpdateVertex()
    {
        if (totalInputIdx >= 0)
        {
            var cameraPos = Camera.main.transform.position;
            cs.SetFloats("_CameraPos", cameraPos.x, cameraPos.y, cameraPos.z);
            cs.SetFloat("_Life", Mathf.Min(time, Time.time - startTime));
            cs.SetInt("_TotalInputIdx", totalInputIdx);
            cs.SetInt("_BufferSize", bufferSize);
            cs.SetFloat("_Time", Time.time);
            cs.SetFloat("_StartWidth", startWidth);
            cs.SetFloat("_EndWidth", endWidth);
            cs.SetFloats("_StartColor", startColor.r, startColor.g, startColor.b, startColor.a);
            cs.SetFloats("_EndColor",   endColor.r, endColor.g, endColor.b, endColor.a);

            var kernel = cs.FindKernel("CreateWidth");
            cs.SetBuffer(kernel, "inputBuffer", inputBuffer);
            cs.SetBuffer(kernel, "pointBuffer", pointBuffer);
            cs.SetBuffer(kernel, "vertexBuffer", vertexBuffer);

            cs.Dispatch(kernel, Mathf.CeilToInt((float)bufferSize / NUM_THREAD_X), 1, 1);


            kernel = cs.FindKernel("CreatePolygon");
            cs.SetBuffer(kernel, "pointBuffer", pointBuffer);
            cs.SetBuffer(kernel, "vertexBuffer", vertexBuffer);
            cs.Dispatch(kernel, Mathf.CeilToInt((float)bufferSize / NUM_THREAD_X), 1, 1);
        }
    }


    void OnRenderObject()
    {
        if (totalInputIdx >= 2)
        {
            material.SetBuffer("vertexBuffer", vertexBuffer);
            material.SetPass(0);

            var drawPointNum = Mathf.Min(bufferSize, totalInputIdx) - 1;
            Graphics.DrawProcedural(MeshTopology.Triangles, drawPointNum * 6);
        }
    }

    public void OnDestroy()
    {
        ReleaseBuffer();
    }
}
