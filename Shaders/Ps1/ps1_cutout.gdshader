shader_type spatial;
render_mode blend_mix, cull_back, diffuse_lambert, specular_disabled;

#include "res://Shaders/Ps1/ps1_common.gdshaderinc"

// Wie ps1_surface, aber der Alphakanal der Textur schneidet Löcher ins Netz, etwa zwischen die Rippen
uniform float alpha_cutoff : hint_range(0.0, 1.0) = 0.5;

void fragment() {
	vec4 texel = sample_texel(affine_uv);

	ALBEDO = albedo.rgb * texel.rgb * tint;
	ALPHA = texel.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_cutoff;
	ROUGHNESS = 1.0;
	METALLIC = 0.0;
	EMISSION = emission.rgb * emission_energy;
}
