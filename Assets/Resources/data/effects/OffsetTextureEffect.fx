//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

float4x4 worldViewProjection;
float opacity = 1.0;
float2 offset;

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

VSOutput OffsetTextureVS(VSInput input)
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

float4 OffsetTexturePS(VSOutput input, uniform bool useColor, uniform bool useTexture) : COLOR
{
   float4 color;
   
   // if this pixel should not be drawn simply return transparent
   if (input.texCoord.x > offset.x || input.texCoord.y > offset.y)
   {
      color = (float4)0;
   }
   // else get an offset sample from the texture and colorize it
   else
   {
      float2 t = input.texCoord + ((float2)1 - offset);
      color = tex2D(baseTextureSampler, t);

	  if (useColor)
	  {
		 color *= input.color;
	  }
	}
   
	// Return the final colour of the pixel
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
        VertexShader = compile vs_2_0 OffsetTextureVS();
        PixelShader  = compile ps_2_0 OffsetTexturePS(true, false);
    }
}

technique TexturedTechnique
{
    pass P0
    {
        VertexShader = compile vs_2_0 OffsetTextureVS();
        PixelShader  = compile ps_2_0 OffsetTexturePS(false, true);
    }
}

technique ColorTextureBlendTechnique
{
    pass P0
    {
        VertexShader = compile vs_2_0 OffsetTextureVS();
        PixelShader  = compile ps_2_0 OffsetTexturePS(true, true);
    }
}

technique CopyTechnique
{
    pass P0
    {
        VertexShader = compile vs_2_0 OffsetTextureVS();
        PixelShader  = compile ps_2_0 CopyPS(); 
    }
}
