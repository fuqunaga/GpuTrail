using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GpuTrailSystem
{
    public abstract class GpuTrailAppendNode : MonoBehaviour, IGpuTrailAppendNode
    {
        public static class CsParam
        {
            public const string KernelAppendNode = "AppendNode";
            public const string KeywordIgnoreOrigin = "IGNORE_ORIGIN";
            public const string KeywordColorEnable = "COLOR_ENABLE";

            public static readonly int InputCount = Shader.PropertyToID("_InputCount");
            public static readonly int InputBufferPos = Shader.PropertyToID("_InputBuffer_Pos");
            public static readonly int InputBufferColor = Shader.PropertyToID("_InputBuffer_Color");
        }


        [SerializeField]
        protected GpuTrail gpuTrail;
        public GpuTrail GpuTrail => gpuTrail;


        public ComputeShader appendNodeCS;
        public bool colorEnable;
        [Tooltip("Ignore (0,0,0) position input")]
        public bool ignoreOriginInput = true;
        [Tooltip("Input position count per trail")]
        public int inputCountMax = 1;

        public GraphicsBuffer InputBufferPos { get; protected set; }
        public GraphicsBuffer InputBufferColor { get; protected set; }

        protected int BufferSize => gpuTrail.trailNum  * inputCountMax;

        #region Unity

        protected virtual void OnDestroy()
        {
            gpuTrail?.Dispose();
            ReleaseBuffers();
        }

        #endregion


        protected void InitBuffers()
        {
            ReleaseBuffers();

            var size = BufferSize;
            InputBufferPos = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size, Marshal.SizeOf<Vector3>());
            InputBufferPos.SetData(Enumerable.Repeat(default(Vector3), size).ToArray());

            if (colorEnable)
            {
                InputBufferColor = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size, Marshal.SizeOf<Color>());
                InputBufferColor.SetData(Enumerable.Repeat(Color.gray, size).ToArray());
            }
        }

        protected void ReleaseBuffers()
        {
            if (InputBufferPos != null) InputBufferPos.Release();
            if (InputBufferColor != null) InputBufferColor.Release();
        }

        /// <summary>
        /// return true if inputBuffer has updated.
        /// </summary>
        /// <returns>max input count of trail</returns>
        protected abstract int UpdateInputBuffer();

        public virtual void AppendNode()
        {
            if (!gpuTrail.IsInitialized)
            {
                PreInitGpuTrail();
                gpuTrail.Init();
                InitBuffers();
            }

            var inputCount = UpdateInputBuffer();
            if (inputCount > 0)
            {
                DispatchAppendNode(inputCount);
            }
        }
        
        protected virtual void PreInitGpuTrail() {}


        public void SetCSParams(ComputeShader cs, int kernel, int inputCount)
        {
            gpuTrail.SetCSParams(cs, kernel);


            SetKeyword(cs, CsParam.KeywordColorEnable, colorEnable);
            SetKeyword(cs, CsParam.KeywordIgnoreOrigin, ignoreOriginInput);

            cs.SetInt(CsParam.InputCount, inputCount);
            cs.SetBuffer(kernel, CsParam.InputBufferPos, InputBufferPos);
            if (colorEnable)
            {
                cs.SetBuffer(kernel, CsParam.InputBufferColor, InputBufferColor);
            }
        }

        void SetKeyword(ComputeShader cs, string keyword, bool flag)
        {
            if (flag)
            {
                cs.EnableKeyword(keyword);
            }
            else
            {
                cs.DisableKeyword(keyword);
            }
        }


        public void DispatchAppendNode(int inputCount)
        {
            var kernel = appendNodeCS.FindKernel(CsParam.KernelAppendNode);
            SetCSParams(appendNodeCS, kernel, inputCount);

            ComputeShaderUtility.Dispatch(appendNodeCS, kernel, gpuTrail.trailNum);

            /*
            var inputPos = new Vector3[inputBuffer_Pos.count];
            inputBuffer_Pos.GetData(inputPos);

            var nodes = new Node[nodeBuffer.count];
            nodeBuffer.GetData(nodes);
            nodes = nodes.Take(100).ToArray();
            */
        }



        #region Debug

        public float gizmosSize = 0.5f;
        public bool gizmosDrawInputPos;
        public bool gizmosDrawNodePos;

        public virtual void OnDrawGizmosSelected()
        {
            if (gizmosDrawInputPos)
            {
                Gizmos.color = Color.red;
                DrawGizmosInputPos(gizmosSize);
            }

            if (gizmosDrawNodePos)
            {
                Gizmos.color = Color.green;
                gpuTrail.DrawGizmosNodePos(gizmosSize);
            }
        }


        public void DrawGizmosInputPos(float radius)
        {
            if (InputBufferPos != null)
            {
                var data = new Vector3[InputBufferPos.count];
                InputBufferPos.GetData(data);

                foreach (var t in data)
                {
                    Gizmos.DrawWireSphere(t, radius);
                }
            }
        }

        #endregion
    }
}