shader_type spatial;
render_mode blend_mix, cull_back, diffuse_lambert, specular_disabled;

#include "res://Shaders/Ps1/ps1_common.gdshaderinc"

void fragment() {
	ALBEDO = sample_surface(affine_uv);

	// Boden und Mauern tragen ihre Textur nach der Welt, Gegenstände und Effekte nach dem Netz
	if (world_uv_scale > 0.0) {
		ALBEDO = mute_environment(ALBEDO);
	}

	ROUGHNESS = 1.0;
	METALLIC = 0.0;
	EMISSION = emission.rgb * emission_energy;
}
