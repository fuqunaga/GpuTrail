using UnityEngine;
using System.Linq;
using UnityEngine.Assertions;
using System.Collections.Generic;

/// <summary>
/// GPUTrailIndirectCulling
/// cull processing for many camera(same position because GPUTrail faces one point)
/// </summary>
public abstract class GPUTrailIndirectCulling : GPUTrailIndirect
{
    #region TypeDefine
    public class Data
    {
        public ComputeBuffer _trailIsInViews;
        public ComputeBuffer _trailIsInViewsAppend;
        public ComputeBuffer _trailIsInViewArgs;


        public Data(int trailNumMax, int indexNumPerTrail, int vertexBuferSize)
        {
            _trailIsInViews = new ComputeBuffer(trailNumMax, 4); // bool buffer but stride must be amultiple of 4
            _trailIsInViewsAppend = new ComputeBuffer(trailNumMax, sizeof(uint), ComputeBufferType.Append);
            _trailIsInViewArgs = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
            _trailIsInViewArgs.SetData(new[] { indexNumPerTrail, 1, 0, 0 });
        }

        public void Release()
        {
            new[] { _trailIsInViews, _trailIsInViewsAppend, _trailIsInViewArgs }
            .Where(b => b != null)
            .ToList()
            .ForEach(b => b.Release());
        }

        const int NUM_THREAD_X = 32;
        public void Update(ComputeShader cs, Camera camera, ComputeBuffer nodeBuffer, ComputeBuffer vertexBufferOrig)
        {
            var kernel = cs.FindKernel("ClearIsInView");
            cs.SetBuffer(kernel, "_IsInViewW", _trailIsInViews);
            cs.Dispatch(kernel, Mathf.CeilToInt((float)_trailIsInViews.count / NUM_THREAD_X), 1, 1);

            kernel = cs.FindKernel("UpdateIsInView");
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var normals = planes.Take(4).Select(p => p.normal).ToList();
            planes.Take(4).ToList().ForEach(plane => Debug.DrawRay(camera.transform.position, plane.normal* 10f));
            var normalsFloat = Enumerable.Range(0, 3).SelectMany(i => normals.Select(n => n[i])).ToArray(); // row major -> column major
            cs.SetFloats("_CameraFrustumNormals", normalsFloat);
            cs.SetBuffer(kernel, "_IsInViewW", _trailIsInViews);
            cs.SetBuffer(kernel, "_NodeBuffer", nodeBuffer);
            cs.Dispatch(kernel, Mathf.CeilToInt((float)nodeBuffer.count / NUM_THREAD_X), 1, 1);


            _trailIsInViewsAppend.SetCounterValue(0);

            kernel = cs.FindKernel("UpdateTrailAppend");
            cs.SetBuffer(kernel, "_IsInView", _trailIsInViews);
            cs.SetBuffer(kernel, "_IsInViewAppend", _trailIsInViewsAppend);
            cs.Dispatch(kernel, Mathf.CeilToInt((float)_trailIsInViews.count / NUM_THREAD_X), 1, 1);

            ComputeBuffer.CopyCount(_trailIsInViewsAppend, _trailIsInViewArgs, 4); // int[4]{ indexNumPerNode, trailNum, 0, 0}
        }
    }
    #endregion

    public ComputeShader _cullingCS;
    public bool _cullingEnable = true;

    protected override void OnRenderObjectInternal()
    {
        var cam = Camera.current;
        if (_cullingEnable && !cam.orthographic)
        {
            if (!_cameraDatas.ContainsKey(cam))
            {
                _cameraDatas[cam] = null; // このフレームは登録だけ
            }
            else {
                var data = _cameraDatas[cam];
                if (data != null)
                {

                    _material.EnableKeyword("GPUTRAIL_TRAIL_INDEX_ON");
                    setCommonMaterialParam();

                    _material.SetBuffer("_TrailIndexBuffer", data._trailIsInViewsAppend);
                    _material.SetPass(0);

                    Graphics.DrawProceduralIndirect(MeshTopology.Triangles, data._trailIsInViewArgs);
                }
            }
        }
        else
        {
            base.OnRenderObjectInternal();
        }
    }


    protected override void LateUpdate()
    {
        base.LateUpdate();

        if (_cullingEnable)
        {
            _cameraDatas.Keys.Where(cam => cam == null).ToList().ForEach(cam => _cameraDatas.Remove(cam));
            _cameraDatas.Keys
                .Where(cam => cam.isActiveAndEnabled && !cam.orthographic)
                .ToList().ForEach(cam =>
            {
                UpdateVertexBuffer(cam);
            });
        }
    }


    Dictionary<Camera, Data> _cameraDatas = new Dictionary<Camera, Data>();
    void UpdateVertexBuffer(Camera camera)
    {
#if UNITY_EDITOR
        if (cameraPos != camera.transform.position) {
            Debug.LogWarning("GPUTrail faces to main camera. but different position camera detected! culling operation is uncertain.");
        }
#endif

        Data data = _cameraDatas[camera];
        if ( data == null )
        {
            data = _cameraDatas[camera] = new Data(trailNumMax, indexNumPerTrail, vertexBufferSize);
        }

        _SetCommonParameterForCS(_cullingCS);
        data.Update(_cullingCS, camera, _nodeBuffer, _vertexBuffer);
    }

    protected override void ReleaseBuffer()
    {
        base.ReleaseBuffer();

        _cameraDatas.Values.Where(d => d!=null).ToList().ForEach(d => d.Release());
        _cameraDatas.Clear();
    }
}
