using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GpuTrailSystem
{
    public abstract class GpuTrailAppendNode : MonoBehaviour, IGpuTrailAppendNode
    {
        public static class CSParam
        {
            public static readonly string Kernel_AppendNode = "AppendNode";
            public static readonly string Keyword_IgnoreOrigin = "IGNORE_ORIGIN";
            public static readonly string Keyword_ColorEnable = "COLOR_ENABLE";

            public static readonly int InputCount = Shader.PropertyToID("_InputCount");
            public static readonly int InputBuffer_Pos = Shader.PropertyToID("_InputBuffer_Pos");
            public static readonly int InputBuffer_Color = Shader.PropertyToID("_InputBuffer_Color");
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

        public GraphicsBuffer inputBuffer_Pos { get; protected set; }
        public GraphicsBuffer inputBuffer_Color { get; protected set; }

        protected int bufferSize => gpuTrail.trailNum  * inputCountMax;

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

            var size = bufferSize;
            inputBuffer_Pos = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size, Marshal.SizeOf<Vector3>());
            inputBuffer_Pos.SetData(Enumerable.Repeat(default(Vector3), size).ToArray());

            if (colorEnable)
            {
                inputBuffer_Color = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size, Marshal.SizeOf<Color>());
                inputBuffer_Color.SetData(Enumerable.Repeat(Color.gray, size).ToArray());
            }
        }

        protected void ReleaseBuffers()
        {
            if (inputBuffer_Pos != null) inputBuffer_Pos.Release();
            if (inputBuffer_Color != null) inputBuffer_Color.Release();
        }

        /// <summary>
        /// return true if inputBuffer has updated.
        /// </summary>
        /// <returns>max input count of trail</returns>
        protected abstract int UpdateInputBuffer();

        public virtual void AppendNode()
        {
            if (!gpuTrail.isInitialized)
            {
                gpuTrail.Init();
                InitBuffers();
            }

            var inputCount = UpdateInputBuffer();
            if (inputCount > 0)
            {
                DispatchAppendNode(inputCount);
            }
        }



        public void SetCSParams(ComputeShader cs, int kernel, int inputCount)
        {
            gpuTrail.SetCSParams(cs, kernel);


            SetKeyword(cs, CSParam.Keyword_ColorEnable, colorEnable);
            SetKeyword(cs, CSParam.Keyword_IgnoreOrigin, ignoreOriginInput);

            cs.SetInt(CSParam.InputCount, inputCount);
            cs.SetBuffer(kernel, CSParam.InputBuffer_Pos, inputBuffer_Pos);
            if (colorEnable)
            {
                cs.SetBuffer(kernel, CSParam.InputBuffer_Color, inputBuffer_Color);
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
            var kernel = appendNodeCS.FindKernel(CSParam.Kernel_AppendNode);
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
            var datas = new Vector3[inputBuffer_Pos.count];
            inputBuffer_Pos.GetData(datas);

            for (var i = 0; i < datas.Length; ++i)
            {
                Gizmos.DrawWireSphere(datas[i], radius);
            }
        }

        #endregion
    }
}