shader_type spatial;
render_mode blend_mix, cull_back, diffuse_lambert, specular_disabled;

#include "res://Shaders/Ps1/ps1_common.gdshaderinc"

// Wer weiter vom Helden entfernt steht, als er sieht, bleibt verborgen. Am Rand blendet er mit dem Punktmuster der Mauern ein.
// Dieselbe Rechnung steht in SightRange. Ein Radius von 0 hebt die Grenze auf
uniform vec3 sight_center = vec3(0.0);
uniform float sight_radius = 0.0;
uniform float sight_edge = 1.5;

void fragment() {
	if (sight_radius > 0.0) {
		vec3 world = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
		float hidden = smoothstep(sight_radius - max(sight_edge, 0.0001), sight_radius, distance(world.xz, sight_center.xz));

		if (is_cut(hidden, SCREEN_UV)) {
			discard;
		}
	}

	ALBEDO = sample_surface(affine_uv);
	ROUGHNESS = 1.0;
	METALLIC = 0.0;
	EMISSION = emission.rgb * emission_energy;
}
