//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

float4x4 worldViewProjection;
float opacity = 1.0;
float4 innerColor;
float4 outerColor;

texture baseTexture;

sampler2D baseTextureSampler = sampler_state
{
	Texture = <baseTexture>;
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
};

sampler CopyTextureSampler = sampler_state
{
    Texture = <baseTexture>;
    MipFilter = POINT;
    MinFilter = POINT;
    MagFilter = POINT;
};

struct VSInput
{
	float4 position : POSITION;
    float4 color : COLOR;
	float2 texCoord : TEXCOORD0;
};

struct VSOutput
{
	float4 position : POSITION;
    float4 color : COLOR0;
	float2 texCoord : TEXCOORD0;
};

VSOutput RadialGradientVS(VSInput input)
{
	VSOutput output;
	output.position = mul(input.position, worldViewProjection);
	output.texCoord.x = input.texCoord.x + (0.5 / 1280);		// adjust the pixel coordinates so it renders 2D images more accurately - later we can have the screen (0.5 * res) as vars if we need to
	output.texCoord.y = input.texCoord.y + (0.5 / 720);
//	output.texCoord = input.texCoord;
	output.color = input.color;
	output.color.a *= opacity;
	return output;
}

float4 RadialGradientPS(VSOutput input, uniform bool useColor, uniform bool useTexture) : COLOR
{
   float4 color;
   
   float2 pos = float2(input.texCoord.x - 0.5, input.texCoord.y - 0.5);
   color = lerp(innerColor, outerColor, length(pos));
   
	if (useColor)
	{
		color *= input.color;
	}
	
	return color;
}

float4 CopyPS(VSOutput input) : COLOR
{
    float4 color = tex2D(baseTextureSampler, input.texCoord);
    color.a = opacity;
    return color;
}

technique ColoredTechnique
{
    pass P0
    {
        VertexShader = compile vs_2_0 RadialGradientVS();
        PixelShader  = compile ps_2_0 RadialGradientPS(true, false);
    }
}

technique TexturedTechnique
{
    pass P0
    {
        VertexShader = compile vs_2_0 RadialGradientVS();
        PixelShader  = compile ps_2_0 RadialGradientPS(false, true);
    }
}

technique ColorTextureBlendTechnique
{
    pass P0
    {
        VertexShader = compile vs_2_0 RadialGradientVS();
        PixelShader  = compile ps_2_0 RadialGradientPS(true, true);
    }
}

technique CopyTechnique
{
    pass P0
    {
        VertexShader = compile vs_2_0 RadialGradientVS();
        PixelShader  = compile ps_2_0 CopyPS(); 
    }
}
