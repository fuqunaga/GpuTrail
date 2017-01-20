#ifndef GPUTRAIL_VARIABLES_INCLUDED
#define GPUTRAIL_VARIABLES_INCLUDED

#include "GPUTrailVertex.cginc"

fixed4 _StartColor;
fixed4 _EndColor;
StructuredBuffer<uint> _IndexBuffer;
StructuredBuffer<Vertex> _VertexBuffer;

Vertex GetVertex(uint id){
	return _VertexBuffer[_IndexBuffer[id]];
}

#endif // GPUTRAIL_VARIABLES_INCLUDED
