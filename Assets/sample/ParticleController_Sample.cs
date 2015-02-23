using UnityEngine;
using System.Collections;
using System.Threading;



public class ParticleController_Sample : MonoBehaviour
{


    public Texture2D particleTexture;

    public Shader shader;
    private Material mat;

    public float PScale = 1.0f;

    public ComputeShader cs;

    public int particleCount = 1000;
    public Transform OriginLocation;


    private Vector3[] particleLocations;
    private Vector3[] particleColors;
    private Vector3[] particleVelocities;

    private ComputeBuffer particleBuffer;
    private ComputeBuffer particleColorBuffer;
    private ComputeBuffer velocityBuffer;




   //Thread csThread;


    void Start()
    {

        particleLocations = new Vector3[particleCount];
        particleColors = new Vector3[particleCount];
        particleVelocities = new Vector3[particleCount];


        for (int i = 0; i < particleCount; i++)
        {

            particleLocations[i] = OriginLocation.position + Random.insideUnitSphere;
            particleVelocities[i] = Random.insideUnitSphere;
            particleColors[i] = new Vector3(Random.value, Random.value, Random.value);


        }

        particleBuffer = new ComputeBuffer(particleCount, 12);
        particleBuffer.SetData(particleLocations);

        velocityBuffer = new ComputeBuffer(particleCount, 12);
        velocityBuffer.SetData(particleVelocities);

        particleColorBuffer = new ComputeBuffer(particleCount, 12);
        particleColorBuffer.SetData(particleColors);





        mat = new Material(shader);
        mat.hideFlags = HideFlags.HideAndDontSave;

        mat.SetTexture("_Sprite", particleTexture);
        mat.SetFloat("Size", PScale);

    }

    private void ReleaseResources()
    {
        //csThread.Abort();

        particleBuffer.Release();
        particleColorBuffer.Release();
        velocityBuffer.Release();

        Object.DestroyImmediate(mat);
    }

    void OnDisable()
    {
        ReleaseResources();
    }


    void Update()
    {


        cs.SetFloat("Time", Time.timeSinceLevelLoad);
        cs.SetVector("Origin", OriginLocation.position);

        cs.SetBuffer(cs.FindKernel("CSMain"), "colBuffer", particleColorBuffer);
        cs.SetBuffer(cs.FindKernel("CSMain"), "posBuffer", particleBuffer);
        cs.SetBuffer(cs.FindKernel("CSMain"), "velBuffer", velocityBuffer);

        cs.Dispatch(cs.FindKernel("CSMain"), particleCount, 1, 1);

    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {


        Graphics.Blit(src, dst);

        mat.SetBuffer("particleBuffer", particleBuffer);
        mat.SetBuffer("particleColor", particleColorBuffer);
        mat.SetPass(0);

        Graphics.DrawProcedural(MeshTopology.Triangles, particleCount);




    }
}