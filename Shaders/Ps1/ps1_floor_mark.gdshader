shader_type spatial;
render_mode blend_mix, cull_back, diffuse_lambert, specular_disabled;

// Eine Spur auf dem Boden, etwa ein Pentagramm. Sieht aus wie ps1_surface, der Alphakanal des Bilds schneidet sie aus

#include "res://Shaders/Ps1/ps1_common.gdshaderinc"

uniform float alpha_cutoff : hint_range(0.0, 1.0) = 0.5;

void fragment() {
	vec4 texel = sample_texel(affine_uv);

	ALPHA = texel.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_cutoff;
	ALBEDO = albedo.rgb * texel.rgb * tint;
	ROUGHNESS = 1.0;
	METALLIC = 0.0;
	EMISSION = emission.rgb * emission_energy;
}
