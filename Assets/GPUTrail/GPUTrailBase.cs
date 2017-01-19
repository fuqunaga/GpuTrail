using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;

public abstract class GPUTrailBase : MonoBehaviour
{
    #region TypeDefine

    public struct Node
    {
        public Vector3 pos;
        public float time;
    }

    public struct Vertex
    {
        public Vector3 pos;
        public Vector2 uv;
    }

    #endregion


    public ComputeShader cs;
    public Material _material;
    public float _life = 1f;
    public float _startWidth = 1f;
    public float _endWidth = 1f;

    protected float _startTime;

    protected ComputeBuffer _nodeBuffer;
    protected ComputeBuffer _vertexBuffer;
    protected ComputeBuffer _indexBuffer;


    protected abstract int trailNumMax { get; }
    protected abstract int nodeNumPerTrail { get; }

    protected virtual void Awake()
    {
        ReleaseBuffer();
        InitBuffer();
    }

    protected virtual void Start()
    {
        _startTime = Time.time;
    }

    protected virtual void InitBuffer()
    {
        var nodeBufferSize = trailNumMax * nodeNumPerTrail;
        _nodeBuffer = new ComputeBuffer(nodeBufferSize, Marshal.SizeOf(typeof(Node)));
        _vertexBuffer = new ComputeBuffer(nodeBufferSize * 2, Marshal.SizeOf(typeof(Vertex))); // 1 node to 2 vtx(left,right)

        // 各Nodeの最後と次のNodeの最初はポリゴンを繋がないので-1
        var indexData = new int[(nodeBufferSize - 1) * 6];
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

        _indexBuffer = new ComputeBuffer(indexData.Length, Marshal.SizeOf(typeof(uint))); // 1 node to 2 triangles(6vertexs)
        _indexBuffer.SetData(indexData);
    }



    protected virtual void ReleaseBuffer()
    {
        new[] { _nodeBuffer, _vertexBuffer, _indexBuffer }
            .Where(b => b != null)
            .ToList().ForEach(buffer =>
            {
                buffer.Release();
            });
    }
    

    protected virtual Vector3 cameraPos { get { return Camera.main.transform.position; } }

    protected void SetCommonParameterForCS()
    {
        cs.SetInt("_TrailNum", trailNumMax);
        cs.SetInt("_NodeNumPerTrail", nodeNumPerTrail);


        cs.SetFloat("_Time", Time.time);
        cs.SetFloat("_Life", _life);

        var cPos = cameraPos;
        cs.SetFloats("_CameraPos", cPos.x, cPos.y, cPos.z);
        cs.SetFloat("_StartWidth", _startWidth);
        cs.SetFloat("_EndWidth", _endWidth);
    }


    protected virtual void LateUpdate()
    {
        UpdateVertex();
    }

    protected abstract void UpdateVertex();


    void OnRenderObject()
    {
        if ( (Camera.current.cullingMask & (1 << gameObject.layer)) == 0)
        {
            return;
        }

        OnRenderObjectInternal();
    }


    protected virtual void setMaterilParam() { }

    protected virtual void OnRenderObjectInternal()
    {
        setMaterilParam();
        _material.SetBuffer("_IndexBuffer", _indexBuffer);
        _material.SetBuffer("_VertexBuffer", _vertexBuffer);
        _material.SetPass(0);

        /*
        var drawPointNum = Mathf.Min(_bufferSize, _totalInputIdx) - 1;
        var vertexCount = drawPointNum * 6;
        */
        Graphics.DrawProcedural(MeshTopology.Triangles, _indexBuffer.count);

        /*
        _material.SetPass(1);
        Graphics.DrawProcedural(MeshTopology.Triangles, vertexCount);
        */
    }

    public void OnDestroy()
    {
        ReleaseBuffer();
    }
}
