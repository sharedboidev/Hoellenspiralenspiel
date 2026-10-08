shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_never, cull_back, shadows_disabled, fog_disabled;

// Eine Kruste aus Eis als Overlay über jedem Netz einer Einheit mit Brittle, siehe BrittleCrust. Sie liegt ein wenig außen auf,
// ist zur Kante hin heller und zeigt Risse. Durchscheinend, so bleibt der Umriss aus ps1_unit_outline unberührt.
// Die Risse folgen den UV, so wandern sie bei der Animation nicht über den Körper
uniform vec4 ice_color : source_color = vec4(0.62, 0.88, 1.0, 1.0);
uniform vec4 rim_color : source_color = vec4(0.95, 1.0, 1.0, 1.0);
uniform float opacity : hint_range(0.0, 1.0) = 0.5;

// So weit liegt die Kruste außen auf, in Einheiten des Netzes
uniform float thickness = 0.015;
uniform float rim_power = 2.0;

// Zellen der Risse je UV-Einheit und ihre Breite
uniform float crack_scale = 9.0;
uniform float crack_width = 0.08;

// Übernimmt BrittleCrust vom Körper darunter, damit beide auf demselben Raster einrasten
uniform float snap = 1.0;
uniform vec2 snap_resolution = vec2(852.0, 480.0);

vec2 hash2(vec2 p) {
	p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));

	return fract(sin(p) * 43758.5453);
}

// Abstand zur nächsten Grenze zweier Zellen: 0 auf einem Riss
float crack_distance(vec2 p) {
	vec2 cell = floor(p);
	vec2 f = fract(p);
	float nearest = 8.0;
	float second = 8.0;

	for (int y = -1; y <= 1; y++) {
		for (int x = -1; x <= 1; x++) {
			vec2 offset = vec2(float(x), float(y));
			vec2 r = offset + hash2(cell + offset) - f;
			float d = dot(r, r);

			if (d < nearest) {
				second = nearest;
				nearest = d;
			} else if (d < second) {
				second = d;
			}
		}
	}

	return sqrt(second) - sqrt(nearest);
}

void vertex() {
	vec4 clip = PROJECTION_MATRIX * (MODELVIEW_MATRIX * vec4(VERTEX + NORMAL * thickness, 1.0));

	if (snap > 0.5) {
		vec2 grid = snap_resolution * 0.5;

		clip.xy = round(clip.xy / clip.w * grid) / grid * clip.w;
	}

	POSITION = clip;
}

void fragment() {
	float facing = clamp(abs(dot(normalize(NORMAL), normalize(VIEW))), 0.0, 1.0);
	float fresnel = pow(1.0 - facing, rim_power);
	float crack = 1.0 - smoothstep(0.0, crack_width, crack_distance(UV * crack_scale));
	float bright = max(fresnel, crack);

	ALBEDO = mix(ice_color.rgb, rim_color.rgb, bright);
	ALPHA = clamp(opacity + fresnel * 0.4 + crack * 0.3, 0.0, 1.0);
}
