#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

sampler2D texSampler;

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 col = tex2D(texSampler, input.TextureCoordinates);
	float avgColor = (col.r + col.g + col.b) / 3;
	float3 mulColor = input.Color.rgb * avgColor;
	return float4(mulColor, col.a);
	//return tex2D(SpriteTextureSampler,input.TextureCoordinates) * input.Color;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};