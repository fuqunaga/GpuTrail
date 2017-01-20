using UnityEngine;
using System.Runtime.InteropServices;


public class GPUTrailIndirectSample : GPUTrailIndirect
{
    struct Particle
    {
        public Vector3 velocity;
        public Vector3 pos;
    }

    ComputeBuffer _particleBuffer;

    [Header("ParticleCS")]
    public ComputeShader _particleCS;
    public float _startSpeed = 1f;
    public float _forceRate = 0.01f;
    public float _gravity = 0.01f;
    public Vector3 _bounds;

    protected override void Awake()
    {
        base.Awake();

        _particleBuffer = new ComputeBuffer(trailNumMax, Marshal.SizeOf(typeof(Particle)));

        var data = new Particle[_particleBuffer.count];
        for (var i = 0; i < data.Length; ++i)
        {
            data[i] = new Particle
            {
                pos = -Vector3.up * _bounds.y * 0.5f,
                velocity = Vector3.Scale(Random.insideUnitSphere, Vector3.up * _startSpeed)
            };
        }

        _particleBuffer.SetData(data);
    }

    Vector3[] inputDatas;

    const int NUM_THREAD_X = 16;
    protected override void UpdateInputBuffer()
    {
        _particleCS.SetFloat("_Time", Time.time);
        _particleCS.SetFloat("_ForceRate", _forceRate);
        _particleCS.SetFloat("_Gravity", _gravity);
        _particleCS.SetFloats("_Bounds", _bounds.x, _bounds.y, _bounds.z);


        var kernel = _particleCS.FindKernel("CSMain");
        _particleCS.SetBuffer(kernel, "_ParticleBuffer", _particleBuffer);
        _particleCS.SetBuffer(kernel, "_InputBuffer", _inputBuffer);

        _particleCS.Dispatch(kernel, Mathf.CeilToInt(_particleBuffer.count / NUM_THREAD_X), 1, 1);
    }

    protected override void ReleaseBuffer()
    {
        base.ReleaseBuffer();

        if (_particleBuffer != null) _particleBuffer.Release();
    }
}