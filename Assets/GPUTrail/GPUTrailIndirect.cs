using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

public class GPUTrailIndirect : MonoBehaviour
{
    struct Input
    {
        public Vector3 pos;
    }
    ComputeBuffer inputBuffer;
    Input[] inputDatas;

    public struct Trail
    {
        public float startTime;
        public int totalInputNum;
    }

    public Material material;
    public int trailNumMax = 1000;
    public float time = 20f;
    public float startWidth = 1f;
    public float endWidth = 1f;
    public Color startColor = Color.white;
    public Color endColor = Color.white;
    public float minVertexDistance = 0.1f;
    public ComputeShader cs;


    int nodeNumPerTrail;

    ComputeBuffer trailBuffer;
    ComputeBuffer nodeBuffer;
    ComputeBuffer vertexBuffer;
    ComputeBuffer vertexBuffer2;
    ComputeBuffer indexBuffer;


    const float FPS = 60f;
    void Start()
    {
        ReleaseBuffer();

        nodeNumPerTrail = Mathf.CeilToInt(time * FPS);
        var bufferSize = trailNumMax * nodeNumPerTrail;

        trailBuffer = new ComputeBuffer(trailNumMax, Marshal.SizeOf(typeof(Trail)));
        trailBuffer.SetData(Enumerable.Repeat(default(Trail), trailNumMax).ToArray());

        nodeBuffer = new ComputeBuffer(bufferSize, Marshal.SizeOf(typeof(GPUTrail.Point)));
        nodeBuffer.SetData(Enumerable.Repeat(default(GPUTrail.Point), bufferSize).ToArray());

        vertexBuffer = new ComputeBuffer(bufferSize * 6, Marshal.SizeOf(typeof(GPUTrail.Vertex))); // 1 node to 2 triangles(6vertexs)
        vertexBuffer.SetData(Enumerable.Repeat(default(GPUTrail.Vertex), bufferSize*6).ToArray());
        // not initialize data. write all buffer every frame.


        vertexBuffer2 = new ComputeBuffer(bufferSize * 2, Marshal.SizeOf(typeof(GPUTrail.Vertex))); // 1 node to 2 triangles(6vertexs)
        vertexBuffer2.SetData(Enumerable.Repeat(default(GPUTrail.Vertex), bufferSize*2).ToArray());


        // 各Nodeの最後と次のNodeの最初はポリゴンを繋がないので-1
        var indexData = new int[trailNumMax * (nodeNumPerTrail-1) * 6];
        var iidx = 0;
        for (var iTrail = 0; iTrail < trailNumMax; ++iTrail)
        {
			var nodeStart = iTrail * nodeNumPerTrail * 2;
            for (var iNode = 0; iNode < nodeNumPerTrail - 1; ++iNode)
            {
				var offset = nodeStart + iNode * 2;
                indexData[iidx++] = 0 + offset;
                indexData[iidx++] = 1 + offset;
                indexData[iidx++] = 2 + offset;
                indexData[iidx++] = 2 + offset;
                indexData[iidx++] = 1 + offset;
                indexData[iidx++] = 3 + offset;
            }
        }

        indexBuffer = new ComputeBuffer(indexData.Length, Marshal.SizeOf(typeof(uint))); // 1 node to 2 triangles(6vertexs)
        indexBuffer.SetData(indexData);

        material.SetBuffer("_Indexes", indexBuffer);



        inputDatas = new Input[trailNumMax];
        for (var i = 0; i < inputDatas.Length; ++i)
        {
            inputDatas[i] = new Input()
            {
                pos = new Vector3(i*5f + 5f, 0f, 0f)
            };
        }
        inputBuffer = new ComputeBuffer(trailNumMax, Marshal.SizeOf(typeof(Input)));
    }

    void ReleaseBuffer()
    {
        new[] { inputBuffer, trailBuffer, nodeBuffer, vertexBuffer, vertexBuffer2, indexBuffer }
            .Where(b => b != null)
            .ToList().ForEach(buffer =>
            {
                buffer.Release();
            });
    }

    void LateUpdate()
    {
        UpdateVertex();
    }

    public bool useIndex;



    protected virtual Vector3 cameraPos { get { return Camera.main.transform.position; } }
    const int NUM_THREAD_X = 16;
    void UpdateVertex()
    {
        cs.SetInt("_TrailNum", trailNumMax);
        cs.SetInt("_NodeNumPerTrail", nodeNumPerTrail);
        cs.SetFloat("_Time", Time.time);

        // AddNode
        for (var i = 0; i < inputDatas.Length; ++i)
        {
            var data = inputDatas[i];
            data.pos.z += 0.1f;
            inputDatas[i] = data;
        }
        inputBuffer.SetData(inputDatas);


        var kernel = cs.FindKernel("AddNode");
        cs.SetBuffer(kernel, "_InputBuffer", inputBuffer);
        cs.SetBuffer(kernel, "_TrailBufferW", trailBuffer);
        cs.SetBuffer(kernel, "_NodeBufferW", nodeBuffer);

        cs.Dispatch(kernel, Mathf.CeilToInt((float)trailBuffer.count / NUM_THREAD_X), 1, 1);



        // CreateWidth
        var cPos = cameraPos;
        cs.SetFloats("_CameraPos", cPos.x, cPos.y, cPos.z);
        cs.SetFloat("_StartWidth", startWidth);
        cs.SetFloat("_EndWidth", endWidth);
        cs.SetVector("_StartColor", startColor);
        cs.SetVector("_EndColor", endColor);

        kernel = cs.FindKernel("CreateWidth");
        cs.SetBuffer(kernel, "_TrailBuffer", trailBuffer);
        cs.SetBuffer(kernel, "_NodeBuffer", nodeBuffer);
        cs.SetBuffer(kernel, "vertexBuffer", vertexBuffer);
        cs.SetBool("_UseIdx", useIndex);
        cs.Dispatch(kernel, Mathf.CeilToInt((float)nodeBuffer.count / NUM_THREAD_X), 1, 1);

        if (!useIndex)
        {
            kernel = cs.FindKernel("CreatePolygon");
            cs.SetBuffer(kernel, "_TrailBuffer", trailBuffer);
            cs.SetBuffer(kernel, "_NodeBuffer", nodeBuffer);
            cs.SetBuffer(kernel, "vertexBuffer", vertexBuffer);
            cs.Dispatch(kernel, Mathf.CeilToInt((float)nodeBuffer.count / NUM_THREAD_X), 1, 1);
        }
    }


    void OnRenderObject()
    {
        setMaterilParam();
        material.SetInt("_UseIdx", useIndex ? 1 : 0);
        material.SetBuffer("vertexBuffer", vertexBuffer);
        material.SetPass(0);

        if (useIndex)
        {

            Graphics.DrawProcedural(MeshTopology.Triangles, indexBuffer.count);
        }
        else {
            Graphics.DrawProcedural(MeshTopology.Triangles, vertexBuffer.count);
        }
    }

    protected virtual void setMaterilParam() { }

    public void OnDestroy()
    {
        ReleaseBuffer();

#if UNITY_EDITOR
        //Destroy(material);
#endif
    }
}
