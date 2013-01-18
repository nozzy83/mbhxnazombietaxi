// The default shader for the game.

// Allows us to sample pixels on the texture being rendered.
sampler TextureSampler : register(s0);

// Color over the sprite this color, with alpha used as the 'amount'. When alpha is 
// 1 the sprite will be completely changed color.
float4 tint; 

float4 main(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    // Look up the texture color.
    float4 tex = tex2D(TextureSampler, texCoord);

	// The standard sprite coloring that XNA does out of the box. This allows for some 
	// tinting, but since it is a multiply, you can never add color.
	tex.rgba *= color;

	// The final tint value uses the color of the tint, but maintains the alpha of
	// the texture since we don't want to color transparent pixels.
	float4 mergedTint = float4(tint.r, tint.g, tint.b, tex.a);

	// Blend from the texture color to the full on tint based on the alpha value.
	tex = lerp(tex, mergedTint, tint.a);

	/*
	// WIP drawing outline around sprite. Mostly works but seems to have issues with texture wrapping.
	if (tex.a == 0)
	{
		for (int y = -1; y <= 1; y += 1)
		{
			for (int x = -1; x <= 1; x += 1)
			{
				float sx = 1.0 / 16.0;
				float sy = 1.0 / 192.0;

				// Sample with the given offsets
				float4 otherColor = tex2D(TextureSampler, texCoord + float2(x * sx, y * sy));

				if(otherColor.a != 0)
				{
					tex.rgba = float4(0,0,0,1);
				}
			}
		}
	}
	*/

    return tex;
}


technique Default
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 main();
    }
}
