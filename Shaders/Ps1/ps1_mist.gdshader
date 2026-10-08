shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_never, cull_disabled, shadows_disabled, fog_disabled;

// Nebel als Stapel flacher Scheiben, Mitte im Ursprung, Radius 1 bei einem PlaneMesh von 2 x 2. Jede Scheibe ist ein Schnitt durch dieselbe Wolke:
// Das Rauschen kommt aus der Weltlage in allen drei Achsen und treibt langsam, so ergeben die Schnitte zusammen ein Volumen
// und der Nebel wandert nicht mit, wenn er wächst. Nach oben wird die Wolke schmaler und dünner, bis top_height. Jede Scheibe wogt auf und ab.
// Durchscheinend und ohne Tiefe, so bleibt der Umriss der Figuren unberührt. Die Dichte stuft er in wenigen Schritten ab, wie die Farben der PS1
uniform vec4 color : source_color = vec4(0.78, 0.92, 1.0, 1.0);

// Deckkraft einer Scheibe. Mehrere übereinander decken mehr
uniform float opacity : hint_range(0.0, 1.0) = 0.36;

// Flecken je Meter und wie schnell sie treiben, in Metern je Sekunde. Das Rauschen steigt dabei langsam auf
uniform float noise_scale = 0.9;
uniform vec3 drift = vec3(0.12, 0.08, 0.05);

// Anteil der Fläche, die Nebel trägt
uniform float coverage : hint_range(0.0, 1.0) = 0.65;

// Anteil des Radius, über den der Rand ausläuft
uniform float edge : hint_range(0.01, 1.0) = 0.35;

// Höhe der Wolke über dem Boden in Metern, oben ist sie top_width so breit wie unten und hat noch top_density ihrer Dichte
uniform float top_height = 1.8;
uniform float top_width : hint_range(0.05, 1.0) = 0.6;
uniform float top_density : hint_range(0.0, 1.0) = 0.45;

// So weit wogt eine Scheibe auf und ab, in Metern, und so schnell
uniform float wobble = 0.25;
uniform float wobble_speed = 0.35;

uniform float steps = 4.0;

// Beim Auflösen steigt die Wolke so weit auf, in Metern, und weitet sich um diesen Anteil
uniform float rise_height = 1.2;
uniform float rise_spread = 0.3;

// Setzt LingeringCloud: density geht beim Aufziehen von 0 auf 1. dissipate geht beim Auflösen von 0 auf 1,
// dabei steigt die Wolke auf und verschwindet von unten nach oben
instance uniform float density = 1.0;
instance uniform float dissipate = 0.0;

varying vec3 world_position;

// Höhe der Scheibe ohne Aufsteigen und Wogen, 0 am Boden und 1 bei top_height
varying float layer_height;

float hash(vec3 p) {
	return fract(sin(dot(p, vec3(127.1, 311.7, 74.7))) * 43758.5453);
}

float value_noise(vec3 p) {
	vec3 cell = floor(p);
	vec3 f = fract(p);
	vec3 u = f * f * (3.0 - 2.0 * f);

	float bottom = mix(mix(hash(cell), hash(cell + vec3(1.0, 0.0, 0.0)), u.x),
	                   mix(hash(cell + vec3(0.0, 0.0, 1.0)), hash(cell + vec3(1.0, 0.0, 1.0)), u.x), u.z);
	float top = mix(mix(hash(cell + vec3(0.0, 1.0, 0.0)), hash(cell + vec3(1.0, 1.0, 0.0)), u.x),
	                mix(hash(cell + vec3(0.0, 1.0, 1.0)), hash(cell + vec3(1.0, 1.0, 1.0)), u.x), u.z);

	return mix(bottom, top, u.y);
}

float fbm(vec3 p) {
	float value = 0.0;
	float amplitude = 0.5;

	for (int i = 0; i < 4; i++) {
		value += amplitude * value_noise(p);
		p *= 2.03;
		amplitude *= 0.5;
	}

	return value;
}

void vertex() {
	vec3 world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;

	layer_height = clamp(world.y / max(top_height, 0.01), 0.0, 1.0);

	float swell = value_noise(vec3(world.xz * 0.6, world.y * 2.0 + TIME * wobble_speed)) - 0.5;

	world.y += swell * 2.0 * wobble + dissipate * rise_height;
	world_position = world;

	POSITION = PROJECTION_MATRIX * VIEW_MATRIX * vec4(world, 1.0);
}

void fragment() {
	float height = layer_height;
	float r = length(UV - 0.5) * 2.0 / (mix(1.0, top_width, height) * (1.0 + dissipate * rise_spread));

	if (r > 1.0) {
		discard;
	}

	vec3 p = world_position * noise_scale + TIME * drift;
	float n = fbm(p) * 0.7 + fbm(p * 2.7 - TIME * drift * 1.7) * 0.3;
	float rim = 1.0 - smoothstep(1.0 - edge, 1.0, r);
	// Beim Auflösen behält die Spitze ihre Dichte, sonst wäre sie weg, bevor sie aufsteigen kann
	float thinning = mix(1.0, top_density, pow(height, 2.5) * (1.0 - dissipate));

	// Die Grenze, unter der nichts mehr bleibt, wandert beim Auflösen vom Boden bis über die Spitze
	float cut = dissipate * 1.5 - 0.5;
	float remaining = clamp((height - cut) / 0.5, 0.0, 1.0) * (1.0 - dissipate * 0.35);

	float amount = clamp((n - (1.0 - coverage)) / max(coverage, 0.001) * 1.6, 0.0, 1.0) * rim * thinning * density * remaining;

	amount = floor(amount * steps + 0.5) / max(steps, 1.0);

	if (amount <= 0.0) {
		discard;
	}

	ALBEDO = color.rgb * (0.8 + 0.2 * n);
	ALPHA = amount * opacity;
}
