
using UnityEngine;
using System.Runtime.InteropServices;


[System.Serializable]
public class GPUTrailIndirectSampleParticle
{
    struct Particle
    {
        public Vector3 velocity;
        public Vector3 pos;
    }

    ComputeBuffer _particleBuffer;


    public ComputeShader _particleCS;
    public int _particleNum = 1000;
    public float _startSpeed = 1f;
    public float _forceRate = 0.01f;
    public float _damping = 0.99f;
    public float _gravity = 0.01f;
    public Vector3 _bounds;

    public void Init()
    {
        _particleBuffer = new ComputeBuffer(_particleNum, Marshal.SizeOf(typeof(Particle)));

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

    const int NUM_THREAD_X = 8;
    public void UpdateInputBuffer(ComputeBuffer inputBuffer)
    {
        _particleCS.SetFloat("_Time", Time.time);
        _particleCS.SetFloat("_ForceRate", _forceRate);
        _particleCS.SetFloat("_Damping", _damping);
        _particleCS.SetFloat("_Gravity", _gravity);
        _particleCS.SetFloats("_Bounds", _bounds.x, _bounds.y, _bounds.z);


        var kernel = _particleCS.FindKernel("CSMain");
        _particleCS.SetBuffer(kernel, "_ParticleBuffer", _particleBuffer);
        _particleCS.SetBuffer(kernel, "_InputBuffer", inputBuffer);

        _particleCS.Dispatch(kernel, Mathf.CeilToInt((float)_particleBuffer.count / NUM_THREAD_X), 1, 1);
    }

    public void ReleaseBuffer()
    {
        if (_particleBuffer != null) _particleBuffer.Release();
    }
}