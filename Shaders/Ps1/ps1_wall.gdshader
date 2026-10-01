shader_type spatial;
render_mode blend_mix, cull_back, diffuse_lambert, specular_disabled;

#include "res://Shaders/Ps1/ps1_common.gdshaderinc"
#include "res://Shaders/Ps1/ps1_wall.gdshaderinc"

void fragment() {
	vec3 world = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;

	if (sees_through(world, MODEL_MATRIX, CAMERA_POSITION_WORLD, FRAGCOORD.xy)) {
		discard;
	}

	ALBEDO = sample_surface(affine_uv);
	ROUGHNESS = 1.0;
	METALLIC = 0.0;
	EMISSION = emission.rgb * emission_energy;
}
