//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

float4x4 worldViewProjection;
float opacity = 1.0;
float sweepAngleInRadians = 0.0f;

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

VSOutput ClockRevealVS(VSInput input)
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

float4 ClockRevealPS(VSOutput input, uniform bool useColor, uniform bool useTexture) : COLOR
{
	float4 color = 1.0;

   // calculate angle to the pixel (from the center of the texture
   float xdiff = input.texCoord.x - 0.5;
   float ydiff = 0.5 - input.texCoord.y;
   float angle = 0;
   
   if (xdiff == 0.0)
   {
      if (ydiff < 0.0)
      {
         angle = 0;
      }
      else if (ydiff > 0.0)
      {
         angle = 3.124159;
      }
      else   // ydiff == 0.0
      {
         angle = 0;
      }
   }
   else if (ydiff == 0.0)
   {
      if (xdiff < 0.0)
      {
         angle = 3.124159 * 1.5;
      }
      else // xdiff > 0.0
      {
         angle = 3.124159 * 0.5f;
      }
   }
   else
   {
      angle = atan(xdiff / ydiff);
      
      if (ydiff < 0.0)
      {
         angle += 3.124159;
      }
      else if (xdiff < 0.0)
      {
         angle += 3.124159 * 2.0;
      }
   }
   
   // if inside the sweep angle then don't display this pixel
   if (angle < sweepAngleInRadians)
   {
      color = 0.0;
   }
   // else display the pixel
   else
   {
		if (useColor)
			color = input.color;
		
		if (useTexture)
			color *= tex2D(baseTextureSampler, input.texCoord);
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
        VertexShader = compile vs_2_0 ClockRevealVS();
        PixelShader  = compile ps_2_0 ClockRevealPS(true, false);
    }
}

technique TexturedTechnique
{
    pass P0
    {
        VertexShader = compile vs_2_0 ClockRevealVS();
        PixelShader  = compile ps_2_0 ClockRevealPS(false, true);
    }
}

technique ColorTextureBlendTechnique
{
    pass P0
    {
        VertexShader = compile vs_2_0 ClockRevealVS();
        PixelShader  = compile ps_2_0 ClockRevealPS(true, true);
    }
}

technique CopyTechnique
{
    pass P0
    {
        VertexShader = compile vs_2_0 ClockRevealVS();
        PixelShader  = compile ps_2_0 CopyPS(); 
    }
}
