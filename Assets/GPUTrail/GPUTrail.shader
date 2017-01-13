Shader "Custom/GPUTrail" {
Properties {
}
   
SubShader {
Pass{
	Cull Off Fog { Mode Off }
	ZWrite Off
	Blend SrcAlpha One

	CGPROGRAM
	#pragma target 5.0

	#pragma vertex vert
	#pragma fragment frag

	#include "UnityCG.cginc"

	struct Vertex
	{
		float3 pos;
		float2 uv;
		float4 color;
	};


	StructuredBuffer<Vertex> vertexBuffer;


	struct vs_out {
		float4 pos : SV_POSITION;
		float4 col : COLOR;
		float2 uv  : TEXCOORD;
	};

	vs_out vert (uint id : SV_VertexID)
	{
		vs_out Out;
		Vertex vtx = vertexBuffer[id];

		Out.pos = mul(UNITY_MATRIX_MVP, float4(vertexBuffer[id].pos, 1.0));
		Out.col = vertexBuffer[id].color;

		Out.uv = vtx.uv;

		return Out;
	}

	fixed4 frag (vs_out In) : COLOR0
	{
		if ( In.uv.x < 0 || 1 < In.uv.x ) discard;
		
		In.col.a *= In.uv.x;
		return In.col;
	}

	ENDCG
   
   }
}

Fallback Off
}

