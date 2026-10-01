shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_never, depth_test_disabled, cull_disabled, fog_disabled, shadows_disabled;

// Liegt als Rechteck über dem ganzen Bild und zieht um Held und Gegner einen dunklen Rand, um den Gegner unter der Maus einen roten.
// Was der Held benutzen kann, bekommt ebenfalls einen dunklen Rand, leuchtet es, einen in einer hellen Fassung seiner Farbe.
// Liegt Leuchtendes hinter etwas, etwa ein Portal in seinem Rahmen, läuft sein Rand innen an der sichtbaren Kante entlang.
// Alle markieren sich im Rauheitskanal, siehe ps1_marks. Das Bild hat das Raster der PS1, jeder Pixel ist eine Zelle,
// so ist der Rand ganze Zellen breit
#include "res://Shaders/Ps1/ps1_marks.gdshaderinc"

uniform sampler2D depth_texture : hint_depth_texture, filter_nearest, repeat_disable;
uniform sampler2D normal_roughness_texture : hint_normal_roughness_texture, filter_nearest, repeat_disable;

// Breite in Zellen des PS1-Rasters, gesetzt von Ps1Look
uniform int outline_width : hint_range(1, 4) = 1;
uniform vec3 outline_color : source_color = vec3(0.02, 0.015, 0.015);
uniform vec3 highlight_color : source_color = vec3(0.9, 0.12, 0.05);

// Der Rand eines leuchtenden Dings hat dessen Farbton bei voller Helligkeit, zu 65 % mit Weiß gemischt.
// Ein Portal leuchtet selbst schon hell, ein satterer Rand ginge in ihm unter
uniform float glow_saturation : hint_range(0.0, 1.0) = 0.35;

void vertex() {
	POSITION = vec4(VERTEX.xy, 1.0, 1.0);
}

float roughness_at(vec2 uv) {
	return texture(normal_roughness_texture, uv).a;
}

// Gemischt in sRGB wie eine Farbe aus dem Inspector, das Bild rechnet linear
vec3 glow_color(float hue) {
	vec3 pure = clamp(abs(mod(hue * 6.0 + vec3(0.0, 4.0, 2.0), 6.0) - 3.0) - 1.0, 0.0, 1.0);
	vec3 srgb = mix(vec3(1.0), pure, glow_saturation);

	return mix(srgb / 12.92, pow((srgb + 0.055) / 1.055, vec3(2.4)), step(0.04045, srgb));
}

void fragment() {
	vec2 texel = 1.0 / VIEWPORT_SIZE;
	int own = mark_kind(roughness_at(SCREEN_UV));

	// Über eine Figur zieht kein Rand. Sonst liefe er innen um jeden Arm
	if (own >= MARK_UNIT) {
		discard;
	}

	float depth = texture(depth_texture, SCREEN_UV).r;
	float reach = float(outline_width * outline_width) + float(outline_width) * 0.5;
	int found = MARK_NONE;
	float hue = 0.0;
	bool is_covered = false;

	// Die Nachbarn in einem runden Umkreis. Bei Breite 1 sind das die vier direkten
	for (int x = -outline_width; x <= outline_width; x++) {
		for (int y = -outline_width; y <= outline_width; y++) {
			if (float(x * x + y * y) > reach || (x == 0 && y == 0)) {
				continue;
			}

			vec2 uv = SCREEN_UV + vec2(float(x), float(y)) * texel;

			// Nur wo der Nachbar vor dem liegt, was hier zu sehen ist. Eine Mauer vor der Figur bekommt keinen Rand
			if (texture(depth_texture, uv).r <= depth) {
				continue;
			}

			float roughness = roughness_at(uv);
			int mark = mark_kind(roughness);

			// Auf ein Ding zieht nur ein Nachbar höheren Rangs einen Rand, etwa der Held davor, nie das Ding selbst
			if (mark > found && mark > own) {
				found = mark;
				hue = mark_hue(roughness);
			} else if (own == MARK_GLOW && mark == MARK_NONE) {
				is_covered = true;
			}
		}
	}

	if (found == MARK_NONE && is_covered) {
		found = MARK_GLOW;
		hue = mark_hue(roughness_at(SCREEN_UV));
	}

	if (found == MARK_NONE) {
		discard;
	}

	if (found == MARK_HIGHLIGHT) {
		ALBEDO = highlight_color;
	} else if (found == MARK_GLOW) {
		ALBEDO = glow_color(hue);
	} else {
		ALBEDO = outline_color;
	}
}
