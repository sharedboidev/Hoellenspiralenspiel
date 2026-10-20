shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, shadows_disabled;

// Der Krater von Lava Crack, flach auf dem Boden auf einem PlaneMesh von 2 x 2 m, Mitte im Ursprung. Zwei Lagen mit demselben Shader:
// Die Mulde (pool_only aus) ist verkohlt, nach innen glüht sie stärker, dazwischen glimmen Funken. Sie liegt mit kleinem lift unter den Rissen.
// Der Pool (pool_only an) ist die brodelnde Lava in der Mitte. Er liegt mit größerem lift über dem Anfang der Risse, so fließen sie aus ihm ab.
// Siehe ps1_common für LIFTED

#define LIFTED

#include "res://Shaders/Ps1/ps1_common.gdshaderinc"

uniform bool pool_only = false;

uniform vec4 core_color : source_color = vec4(1.0, 0.86, 0.38, 1.0);
uniform vec4 lava_color : source_color = vec4(1.0, 0.32, 0.04, 1.0);
uniform vec4 deep_color : source_color = vec4(0.55, 0.07, 0.02, 1.0);
uniform vec4 char_color : source_color = vec4(0.07, 0.04, 0.03, 1.0);
uniform vec4 cooled_color : source_color = vec4(0.2, 0.14, 0.12, 1.0);

// Radius des Pools und der Glut um ihn als Anteil des Kraters
uniform float pool_radius : hint_range(0.05, 1.0) = 0.42;
uniform float glow_radius : hint_range(0.05, 1.0) = 0.72;

// Wie weit der Rand von Mulde und Pool ausfranst
uniform float ragged : hint_range(0.0, 0.5) = 0.14;

// Funken in der Mulde: Anteil der Fläche und ihre Größe je Meter
uniform float ember_share : hint_range(0.0, 1.0) = 0.12;
uniform float ember_scale = 9.0;

// Die Lava im Pool wallt mit diesem Tempo und quillt von der Mitte nach außen
uniform float churn_speed = 0.8;
uniform float steps = 4.0;

// Setzt LavaCrater: heat geht beim Erkalten von 1 auf 0, fade danach von 0 auf 1
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

// Rauschen über dem Winkel, das bei einer ganzen Umdrehung nahtlos anschließt
float ring_noise(float angle, float frequency, float offset) {
	vec2 p = vec2(cos(angle), sin(angle)) * frequency + offset;

	return value_noise(p);
}

vec3 lava_ramp(float hot) {
	hot = floor(clamp(hot, 0.0, 1.0) * steps + 0.5) / steps;

	return hot < 0.5 ? mix(deep_color.rgb, lava_color.rgb, hot * 2.0) : mix(lava_color.rgb, core_color.rgb, hot * 2.0 - 1.0);
}

void fragment() {
	if (is_cut(fade, FRAGCOORD.xy)) {
		discard;
	}

	vec2 p = UV * 2.0 - 1.0;
	float r = length(p);
	float angle = atan(p.y, p.x);

	if (pool_only) {
		float edge = pool_radius * (1.0 - ragged + ragged * ring_noise(angle, 2.5, TIME * 0.4));

		if (r > edge) {
			discard;
		}

		// Von innen quillt sie hell nach außen, wallend
		float churn = value_noise(vec2(angle * 2.0, r * 6.0 - TIME * churn_speed * 3.0));
		float hot = 1.0 - r / edge * 0.7 + (churn - 0.5) * 0.6;

		ALBEDO = mix(mix(char_color.rgb, cooled_color.rgb, churn), lava_ramp(hot), heat);
	} else {
		if (r > 1.0 - ragged * ring_noise(angle, 3.0, 7.0)) {
			discard;
		}

		float glow = 1.0 - smoothstep(pool_radius * 0.8, glow_radius, r);
		float ember = step(1.0 - ember_share, value_noise(UV * ember_scale * 2.0 + floor(TIME * 3.0) * 0.37));
		vec3 bowl = mix(char_color.rgb, deep_color.rgb, glow * 0.9);

		bowl = mix(bowl, lava_color.rgb, ember * (1.0 - r) * 0.8);

		ALBEDO = mix(char_color.rgb, bowl, heat);
	}
}
