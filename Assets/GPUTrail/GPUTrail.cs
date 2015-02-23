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
    }


    public Material material;
    public int bufferSize;
    public float life;
    public float width;
    public float minDistance;

    public int firstIdx;
    public int lastIdx;
    public int currentNum;


    public ComputeBuffer inputBuffer;
    public ComputeBuffer pointBuffer;
    public ComputeBuffer vertexBuffer;


    public ComputeShader cs;


    void Start()
    {
        ReleaseBuffer();

        inputBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Point)));
        pointBuffer = new ComputeBuffer(bufferSize, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Point)));

        vertexBuffer = new ComputeBuffer(bufferSize * 6, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vertex))); // 1 input to 2 triangles(6vertexs)

        AddInput();
    }

    void ReleaseBuffer()
    {
        new[] { inputBuffer, pointBuffer, vertexBuffer }.ToList().ForEach(buffer =>
        {
            buffer.Release();
            buffer = null;
        });
    }

    void Update()
    {
        var pos = transform.position;

        ClearTimeout();

        if ( currentNum <=0 || ( Vector3.Distance(inputArray[currentNum].pos, pos) > minDistance))
        {
            AddInput();
        }

        inputBuffer.SetData(inputArray);

        UpdateVertex();
    }


    void ClearTimeout()
    {
        var oldest = Time.time - life;
        var i = 0;
        for(i = 0; i<currentNum; ++i)
        {
            if (inputArray[i].time >= oldest) break;
        }

        if (i > 0)
        {
            currentNum -= i;
            System.Array.Copy(inputArray, i, inputArray, 0, currentNum);
        }
    }

    void AddInput()
    {
        inputArray[currentNum].pos = transform.position;
        inputArray[currentNum].time = Time.time;

        currentNum++;
    }


    bool canRender { get { return currentNum > 1; } }

    const int NUM_THREAD_X = 16;
    void UpdateVertex()
    {
        if (canRender)
        {
            var cameraPos = Camera.main.transform.position;
            cs.SetFloats("_CameraPos", cameraPos.x, cameraPos.y, cameraPos.z);
            cs.SetFloat("_Life", life);
            cs.SetFloat("_Width", width);
            cs.SetInt("_CurrentNum", currentNum);

            var kernel = cs.FindKernel("CreateWidth");
            cs.SetBuffer(kernel, "inputBuffer", inputBuffer);
            cs.SetBuffer(kernel, "vertexBuffer", vertexBuffer);

            cs.Dispatch(kernel, Mathf.CeilToInt((float)currentNum / NUM_THREAD_X), 1, 1);


            kernel = cs.FindKernel("CreatePolygon");
            cs.SetBuffer(kernel, "vertexBuffer", vertexBuffer);
            cs.Dispatch(kernel, Mathf.CeilToInt((float)currentNum / NUM_THREAD_X), 1, 1);
        }
    }



    void OnRenderObject()
    {
        if (canRender)
        {
            material.SetBuffer("vertexBuffer", vertexBuffer);
            material.SetFloat("_Life", life);
            material.SetFloat("_CurrentNum", currentNum * 6);
            material.SetPass(0);

            Graphics.DrawProcedural(MeshTopology.Triangles, (currentNum-1) * 6);
        }
    }

    public void OnDestroy()
    {
        ReleaseBuffer();
    }
}
