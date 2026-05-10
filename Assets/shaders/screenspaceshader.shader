COMMON
{
    #include "postprocess/shared.hlsl"
}

MODES
{
    Default();
    VrForward();
}

struct VertexInput
{
    float3 pos : POSITION < Semantic( PosXyz ); >;
    float2 uv : TEXCOORD0 < Semantic( LowPrecisionUv ); >;
};

struct PixelInput
{
    float2 uv : TEXCOORD0;
	float4 pos : SV_Position;
};

VS
{
    PixelInput MainVs( VertexInput i )
    {
        PixelInput o;
        
        o.pos = float4(i.pos.xy, 0.0f, 1.0f);
        o.uv = i.uv;
        return o;
    }
}

PS
{
    #include "postprocess/shared.hlsl"


    float3 f < Attribute( "f" ); >;
    Texture2D WaveTexture < Attribute( "waveTex" ); >;
    SamplerState g_sSampler < Filter( MIN_MAG_MIP_LINEAR ); AddressU( CLAMP ); AddressV( CLAMP ); >;

    float4 MainPs( PixelInput i ) : SV_Target0  // Use the SAME struct as VS output
    {
        float2 uv = i.uv;
        float4 sample = WaveTexture.Sample( g_sSampler, uv );
        return sample;
    }
}