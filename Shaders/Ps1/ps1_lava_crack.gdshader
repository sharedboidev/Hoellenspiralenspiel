shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, shadows_disabled;

// Ein Lavariss im Boden, ein flaches Band aus LavaCrack. UV.x ist der Weg ab dem Anfang des Risses in Metern, UV.y geht quer von 0 bis 1.
// Innen fließt glühende Lava den Riss entlang, nach außen geht sie in einen verkohlten Rand über, dessen Kante ausfranst.
// Das Rot der Farbe ist die Glut der Linie: 1 im Riss, weniger in den Haarrissen. LIFTED rückt das Band vor den Boden, siehe ps1_common

#define LIFTED

#include "res://Shaders/Ps1/ps1_common.gdshaderinc"

uniform vec4 core_color : source_color = vec4(1.0, 0.86, 0.38, 1.0);
uniform vec4 lava_color : source_color = vec4(1.0, 0.32, 0.04, 1.0);
uniform vec4 deep_color : source_color = vec4(0.55, 0.07, 0.02, 1.0);
uniform vec4 rim_color : source_color = vec4(0.09, 0.05, 0.04, 1.0);
uniform vec4 cooled_color : source_color = vec4(0.2, 0.14, 0.12, 1.0);

// Anteil der halben Breite, den der verkohlte Rand einnimmt, im Riss und in den Haarrissen
uniform float rim_share : hint_range(0.0, 1.0) = 0.42;
uniform float twig_rim_share : hint_range(0.0, 1.0) = 0.7;

// Wie weit die Kante ausfranst, als Anteil der halben Breite
uniform float ragged : hint_range(0.0, 0.6) = 0.35;

// Die Lava fließt mit diesem Tempo in Metern je Sekunde vom Einschlag weg, ihre Flecken haben diese Größe je Meter
uniform float flow_speed = 0.7;
uniform float flow_scale = 2.2;

// So weit hinter der wachsenden Spitze glüht der Riss heller, in Metern
uniform float tip_glow = 0.8;

uniform float steps = 4.0;

// Setzt LavaCrack: reveal ist die schon gewachsene Länge in Metern, heat geht beim Erkalten von 1 auf 0,
// fade danach von 0 auf 1, dabei zerfällt der Riss im Punktmuster
instance uniform float reveal = 1000.0;
instance uniform float heat = 1.0;
instance uniform float fade = 0.0;

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float value_noise(vec2 p) {
	vec2 cell = floor(p);
	vec2 f = fract(p);
	vec2 u = f * f * (3.0 - 2.0 * f);

	return mix(mix(hash(cell), hash(cell + vec2(1.0, 0.0)), u.x),
	           mix(hash(cell + vec2(0.0, 1.0)), hash(cell + vec2(1.0, 1.0)), u.x), u.y);
}

void fragment() {
	float along = UV.x;

	if (along > reveal || is_cut(fade, FRAGCOORD.xy)) {
		discard;
	}

	// 0 in der Mitte, 1 am Rand
	float across = abs(UV.y * 2.0 - 1.0);
	float glow = COLOR.r;

	// Große Buchten und kleine Zacken, jede Seite für sich
	float edge_noise = 0.65 * value_noise(vec2(along * 2.5, UV.y * 3.0)) + 0.35 * value_noise(vec2(along * 10.0, UV.y * 3.0 + 5.0));

	if (across > 1.0 - ragged * edge_noise) {
		discard;
	}

	float core_edge = 1.0 - mix(twig_rim_share, rim_share, clamp(glow * 2.0 - 1.0, 0.0, 1.0));
	float flow = value_noise(vec2(along * flow_scale - TIME * flow_speed, UV.y * 3.0));
	float wobble = value_noise(vec2(along * 5.0 + 17.0, TIME * 0.5));

	// Wie weit innen in der Lava, 1 in der Mitte, 0 am Rand zur Kohle. Der Rand wogt mit dem Fließen
	float inner = clamp(1.0 - across / max(0.01, core_edge * (0.8 + 0.3 * wobble)), 0.0, 1.0);

	if (inner <= 0.0) {
		ALBEDO = rim_color.rgb;
	} else {
		float hot = clamp(inner * (0.55 + 0.7 * flow), 0.0, 1.0);
		float tip = 1.0 - clamp((reveal - along) / max(0.01, tip_glow), 0.0, 1.0);

		hot = clamp(hot + tip * 0.6, 0.0, 1.0) * mix(0.6, 1.0, glow);
		hot = floor(hot * steps + 0.5) / steps;

		vec3 lava = hot < 0.5 ? mix(deep_color.rgb, lava_color.rgb, hot * 2.0) : mix(lava_color.rgb, core_color.rgb, hot * 2.0 - 1.0);
		vec3 cooled = mix(rim_color.rgb, cooled_color.rgb, flow);

		ALBEDO = mix(cooled, lava, heat);
	}
}
