shader_type spatial;
render_mode blend_mix, cull_back, diffuse_lambert, specular_disabled;

#include "res://Shaders/Ps1/ps1_common.gdshaderinc"

// Mauerwerk gibt im Umkreis des Helden die Sicht frei, wenn er hinter ihm steht.
// Dieselbe Rechnung steht in WallFadeRule, dort entscheidet sie, was sich anklicken lässt
uniform vec3 fade_center = vec3(0.0);
uniform float fade_radius = 0.0;
uniform float fade_edge = 1.5;
uniform float fade_side_ramp = 0.5;

// Wo das Mauerwerk den Helden selbst verdeckt, bleibt es nie ganz zu
uniform float hero_height = 1.8;
uniform float cover_radius = 0.9;
uniform float cover_edge = 0.3;

// Wie weit sich das Stück öffnet, wenn der Held hinter ihm steht: auf der Seite seiner Z-Achse (x) oder auf der anderen (y)
instance uniform vec2 see_through = vec2(1.0, 1.0);

float opening(vec3 world, mat4 model, vec3 camera) {
	// Die dünne Achse des Mauerstücks ist seine Z-Achse
	vec2 wall_origin = model[3].xz;
	vec2 wall_normal = normalize(model[2].xz);
	float hero_side = dot(fade_center.xz - wall_origin, wall_normal);
	float camera_side = dot(camera.xz - wall_origin, wall_normal);

	if (fade_radius <= 0.0 || hero_side * camera_side >= 0.0) {
		return 0.0;
	}

	float amount = hero_side > 0.0 ? see_through.x : see_through.y;
	float behind = smoothstep(0.0, fade_side_ramp, abs(hero_side));
	float outside = smoothstep(fade_radius - fade_edge, fade_radius, distance(world.xz, fade_center.xz));

	return amount * behind * (1.0 - outside);
}

// 1 auf der Sichtlinie von der Kamera zum Körper des Helden, 0 abseits davon und hinter ihm
float cover(vec3 world, vec3 camera) {
	vec3 to_point = world - camera;
	float depth = length(to_point);

	if (depth <= 0.0 || hero_height <= 0.0) {
		return 0.0;
	}

	vec3 ray = to_point / depth;
	vec3 to_camera = camera - fade_center;
	float along = dot(ray, to_camera);
	float spread = 1.0 - ray.y * ray.y;
	float height = spread < 0.0001 ? 0.0 : clamp((to_camera.y - ray.y * along) / spread, 0.0, hero_height);
	float reach = max(0.0, ray.y * height - along);

	if (depth >= reach) {
		return 0.0;
	}

	float gap = distance(camera + ray * reach, fade_center + vec3(0.0, height, 0.0));

	return 1.0 - smoothstep(cover_radius - cover_edge, cover_radius, gap);
}

void fragment() {
	vec3 world = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	float see = max(opening(world, MODEL_MATRIX, CAMERA_POSITION_WORLD), 0.5 * cover(world, CAMERA_POSITION_WORLD));

	if (is_cut(see, SCREEN_UV)) {
		discard;
	}

	ALBEDO = sample_surface(affine_uv);
	ROUGHNESS = 1.0;
	METALLIC = 0.0;
	EMISSION = emission.rgb * emission_energy;
}
