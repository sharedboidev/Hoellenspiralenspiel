// Mauerwerk gibt im Umkreis des Helden die Sicht frei, wenn er hinter ihm steht.
// Dieselbe Rechnung steht in WallFadeRule, dort entscheidet sie, was sich anklicken lässt.
// ps1_wall trägt sie auf das Mauerwerk, ps1_wall_mark auf die Spuren, die daran kleben
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

// Das Licht öffnet nur, was der Held auch sieht. WallFade reicht die Mittellinien der Mauerstücke in seinem Umkreis herein,
// je Stück Anfang (xy) und Ende (zw) am Boden. Steht eines zwischen ihm und der Stelle, bleibt sie zu
uniform int blocker_count = 0;
uniform vec4 blockers[32];

// Wahr, wenn ein Stück die Strecke kreuzt. Das eigene Stück trifft sie genau am Ende und zählt nicht
bool is_blocked(vec2 from, vec2 to) {
	vec2 d = to - from;

	for (int i = 0; i < blocker_count; i++) {
		vec2 a = blockers[i].xy;
		vec2 e = blockers[i].zw - a;
		float denominator = d.x * e.y - d.y * e.x;

		if (abs(denominator) < 0.000001) {
			continue;
		}

		vec2 ap = a - from;
		float along = (ap.x * e.y - ap.y * e.x) / denominator;
		float where = (ap.x * d.y - ap.y * d.x) / denominator;

		if (along > 0.0 && along < 0.999 && where >= 0.0 && where <= 1.0) {
			return true;
		}
	}

	return false;
}

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

	// Geprüft wird der Punkt auf der Mittellinie, der Held sieht ihn von seiner Seite aus
	vec2 on_line = world.xz - wall_normal * dot(world.xz - wall_origin, wall_normal);

	if (outside >= 1.0 || is_blocked(fade_center.xz, on_line)) {
		return 0.0;
	}

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

// Wahr, wo das Punktmuster an dieser Stelle des Bildes die Sicht freigibt, pixel ist FRAGCOORD.xy
bool sees_through(vec3 world, mat4 model, vec3 camera, vec2 pixel) {
	float see = max(opening(world, model, camera), 0.5 * cover(world, camera));

	return is_cut(see, pixel);
}
