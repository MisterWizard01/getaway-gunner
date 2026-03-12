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
	float4 textureColor = tex2D(texSampler, input.TextureCoordinates);
	float4 blendColor = input.Color;
	float3 mulBlend = blendColor.rgb * blendColor.a;
	float3 mulTexture = textureColor.rgb * (1 - blendColor.a);
	return float4(mulTexture + mulBlend, textureColor.a);
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};