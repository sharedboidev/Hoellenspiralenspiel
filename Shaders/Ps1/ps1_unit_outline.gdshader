shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_never, depth_test_disabled, cull_disabled, fog_disabled, shadows_disabled;

// Liegt als Rechteck über dem ganzen Bild und zieht um Held und Gegner einen Rand von einem PS1-Pixel.
// Die Figuren markieren sich im Rauheitskanal, siehe ps1_unit. Entschieden wird pro Zelle des PS1-Rasters,
// so bleibt der Rand genau eine Zelle breit, wenn ps1_screen das Bild vergröbert
uniform sampler2D depth_texture : hint_depth_texture, filter_nearest, repeat_disable;
uniform sampler2D normal_roughness_texture : hint_normal_roughness_texture, filter_nearest, repeat_disable;

uniform vec2 snap_resolution = vec2(426.0, 240.0);
uniform vec3 outline_color : source_color = vec3(0.02, 0.015, 0.015);
uniform vec3 highlight_color : source_color = vec3(0.9, 0.12, 0.05);

// Gesetzt von Ps1Look: Rand um alle Figuren und Rand um den Gegner unter der Maus
global uniform float unit_outline;
global uniform float unit_hover_outline;

const vec2 NEIGHBOURS[4] = vec2[](vec2(1.0, 0.0), vec2(-1.0, 0.0), vec2(0.0, 1.0), vec2(0.0, -1.0));

void vertex() {
	POSITION = vec4(VERTEX.xy, 1.0, 1.0);
}

// 0 Umgebung oder leerer Hintergrund, 1 Figur, 2 Gegner unter der Maus. Die Marken stehen in ps1_unit
int mark_at(vec2 uv) {
	float roughness = texture(normal_roughness_texture, uv).a;

	if (roughness < 0.25 || roughness > 0.85) {
		return 0;
	}

	return roughness < 0.55 ? 1 : 2;
}

void fragment() {
	vec2 cell = floor(SCREEN_UV * snap_resolution);
	vec2 center = (cell + 0.5) / snap_resolution;

	if (mark_at(center) != 0) {
		discard;
	}

	float depth = texture(depth_texture, center).r;
	int found = 0;

	for (int i = 0; i < 4; i++) {
		vec2 uv = (cell + NEIGHBOURS[i] + 0.5) / snap_resolution;
		int mark = mark_at(uv);

		// Nur wo die Figur vor dem liegt, was hier zu sehen ist. Eine Mauer vor der Figur bekommt keinen Rand
		if (mark != 0 && texture(depth_texture, uv).r > depth) {
			found = max(found, mark);
		}
	}

	bool highlighted = found == 2 && (unit_hover_outline > 0.5 || unit_outline > 0.5);
	bool outlined = found == 1 && unit_outline > 0.5;

	if (!highlighted && !outlined) {
		discard;
	}

	ALBEDO = highlighted ? highlight_color : outline_color;
}
