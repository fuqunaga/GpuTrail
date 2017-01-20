Shader "GPUTrail/StartEndColor" {
Properties {
	_StartColor("StartColor", Color) = (1,1,1,1)
	_EndColor("EndColor", Color) = (0,0,0,1)
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
	#include "GPUTrailVariables.cginc"

	struct vs_out {
		float4 pos : SV_POSITION;
		float4 col : COLOR;
		float2 uv  : TEXCOORD;
	};

	vs_out vert (uint id : SV_VertexID)
	{
		vs_out Out;
		Vertex vtx = GetVertex(id);

		Out.pos = mul(UNITY_MATRIX_MVP, float4(vtx.pos, 1.0));
		Out.uv = vtx.uv;
		Out.col = lerp(_EndColor, _StartColor, vtx.uv.x);

		return Out;
	}

	fixed4 frag (vs_out In) : COLOR0
	{
		if (In.uv.x >= 0.999) return fixed4(0,1,0,1);
		return In.col;
	}

	ENDCG
   
   }
}

Fallback Off
}

