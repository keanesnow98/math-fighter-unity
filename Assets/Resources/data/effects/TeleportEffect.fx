//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

float4x4 worldViewProjection;
float opacity = 1.0;
float animTime;
float2 teleportOffset;

texture baseTexture;
texture teleportTexture;


sampler2D baseTextureSampler = sampler_state
{
	Texture = <baseTexture>;
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
};

sampler2D teleportTextureSampler = sampler_state
{
	Texture = <teleportTexture>;
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

VSOutput TeleportVS(VSInput input)
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


float4 TeleportPS(VSOutput input, uniform bool useColor, uniform bool useTexture) : COLOR
{
   float4 color = tex2D(baseTextureSampler, input.texCoord);
   
   if (color.a > 0)   
   {
	   	float timeOffset = floor(animTime * 12.0f) / 12.0f;
 		float4 teleporterColor = tex2D(teleportTextureSampler, float2((input.texCoord.x - teleportOffset.x) * 1.68f, (input.texCoord.y - teleportOffset.y)* 2.906f));
	   		
   		// teleport texture fades in
   		if (animTime <= 0.25f)
   		{
   			teleporterColor.a = (animTime * 4.0f) * color.a;
   			color = teleporterColor;// * teleporterColor.a;
   		}
   		// teleport texture left to animated for a while
   		else if (animTime <= 0.5f)
   		{
   			float time2 = animTime - 0.25f;
   			teleporterColor.a = color.a;
   			color = teleporterColor;
   		}
   		// teleport texture fades out to reveal tutor
   		else	// animTime <= 1.0f;
   		{
   			float time2 = animTime - 0.5f;
   			teleporterColor.a = (1.0f - (time2 * 2.0f)) * color.a;
   			color = (color * (1.0f - teleporterColor.a)) + (teleporterColor * teleporterColor.a);
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
        VertexShader = compile vs_2_0 TeleportVS();
        PixelShader  = compile ps_2_0 TeleportPS(true, false);
    }
}

technique TexturedTechnique
{
    pass P0
    {
        VertexShader = compile vs_2_0 TeleportVS();
        PixelShader  = compile ps_2_0 TeleportPS(false, true);
    }
}

technique ColorTextureBlendTechnique
{
    pass P0
    {
        VertexShader = compile vs_2_0 TeleportVS();
        PixelShader  = compile ps_2_0 TeleportPS(true, true);
    }
}

technique CopyTechnique
{
    pass P0
    {
        VertexShader = compile vs_2_0 TeleportVS();
        PixelShader  = compile ps_2_0 CopyPS(); 
    }
}
