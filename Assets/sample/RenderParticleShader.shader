Shader "Custom/RenderParticles" {
Properties {
   _Sprite ("Sprite", 2D) = "white" {}
}
   
SubShader {
Pass{
   ZWrite Off ZTest Always Cull Off Fog { Mode Off }
   Blend SrcAlpha One



   CGPROGRAM
   #pragma target 5.0

   #pragma vertex vert
   #pragma geometry geom
   #pragma fragment frag

   #include "UnityCG.cginc"
   
   StructuredBuffer<float3> particleBuffer;
   StructuredBuffer<float3> particleColor;

   

   float Size = 0.3f;

   sampler2D _Sprite;
   
   struct vs_out {
      float4 pos : SV_POSITION;
      float4 col : COLOR;
   };
   
   vs_out vert (uint id : SV_VertexID)
   {
      vs_out o;
      o.pos =mul(_Object2World, float4(particleBuffer[id], 1.0f));
      o.col = float4(particleColor[id], 1.0f);
      return o;
   }
   
   struct gs_out {
      float4 pos : SV_POSITION;
      float2 uv  : TEXCOORD0;
      float4 col : COLOR;
   };
   
   [maxvertexcount(4)]
   void geom (point vs_out input[1], inout TriangleStream<gs_out> outStream)
   {

      
      float dx = Size;
      float dy = Size * _ScreenParams.x / _ScreenParams.y;
      gs_out output;
      
           
      float4 corLoc = mul(UNITY_MATRIX_MVP, input[0].pos); 
      output.pos = corLoc + float4(-dx, dy,0,0); output.uv=float2(0,0); 
      output.col = input[0].col; outStream.Append (output);
      output.pos = corLoc + float4( dx, dy,0,0); output.uv=float2(1,0); 
      output.col = input[0].col; outStream.Append (output);
      output.pos = corLoc + float4(-dx,-dy,0,0); output.uv=float2(0,1); 
      output.col = input[0].col; outStream.Append (output);
      output.pos = corLoc + float4( dx,-dy,0,0); output.uv=float2(1,1); 
      output.col = input[0].col; outStream.Append (output);
      

      outStream.RestartStrip();
   }
   
   
      
   fixed4 frag (gs_out i ) : COLOR0
   {
      fixed4 col = tex2D(_Sprite, i.uv);
      col.a = 0.8f;
      col *= i.col;
      return col;
   }
   
   ENDCG
   
   }
}

Fallback Off
}

