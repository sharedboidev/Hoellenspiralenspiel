shader_type spatial;
render_mode blend_mix, cull_back, diffuse_lambert, specular_disabled;

// Eine Spur auf dem Mauerwerk, etwa ein blutiger Handabdruck. Sie öffnet sich mit dem Mauerstück, an dem sie klebt,
// und der Alphakanal ihres Bilds schneidet sie aus. LIFTED rückt sie vor die Fläche, siehe ps1_common

#define LIFTED

#include "res://Shaders/Ps1/ps1_common.gdshaderinc"
#include "res://Shaders/Ps1/ps1_wall.gdshaderinc"

uniform float alpha_cutoff : hint_range(0.0, 1.0) = 0.5;

void fragment() {
	vec3 world = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;

	if (sees_through(world, MODEL_MATRIX, CAMERA_POSITION_WORLD, FRAGCOORD.xy)) {
		discard;
	}

	vec4 texel = sample_texel(affine_uv);

	ALPHA = texel.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_cutoff;
	ALBEDO = albedo.rgb * texel.rgb * tint;
	ROUGHNESS = 1.0;
	METALLIC = 0.0;
	EMISSION = emission.rgb * emission_energy;
}
