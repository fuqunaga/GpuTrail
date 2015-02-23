Shader "Custom/GPUTrail" {
Properties {
   _Life("Life", float)= 1
}
   
SubShader {
Pass{
	Cull Off Fog { Mode Off }
	ZWrite Off
	ZTest Always
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
	};


	StructuredBuffer<Vertex> vertexBuffer;
	uint _CurrentNum;


	struct vs_out {
		float4 pos : SV_POSITION;
		float4 col : COLOR;
	};

	vs_out vert (uint id : SV_VertexID)
	{
		vs_out Out;
		Out.pos = mul(UNITY_MATRIX_MVP, float4(vertexBuffer[id].pos, 1.0));
		//float life_rate = saturate((_Time.y - _InputBuffer[id].time) / _Life);
		float life_rate = (float)id / _CurrentNum;
		Out.col = float4(life_rate, life_rate, life_rate, 1.0);

		return Out;
	}

	fixed4 frag (vs_out In) : COLOR0
	{
		In.col.a *= 0.1;
		return In.col;
	}

	ENDCG
   
   }
}

Fallback Off
}

