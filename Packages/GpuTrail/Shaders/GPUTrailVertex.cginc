#ifndef GPUTRAIL_VARTEX_INCLUDED
#define GPUTRAIL_VARTEX_INCLUDED

struct Vertex
{
    float3 pos;
    float2 uv;
	half4 color;
};

inline Vertex GetDefaultVertex() {

	Vertex ret;
	ret.pos = (0).xxx;
	ret.uv = (-1).xx;
	ret.color = (0).xxxx;

	return ret;
}


#endif // GPUTRAIL_VARTEX_INCLUDED
