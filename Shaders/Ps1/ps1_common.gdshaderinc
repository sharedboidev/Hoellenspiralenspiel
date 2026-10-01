uniform vec4 albedo : source_color = vec4(1.0);
// Mipmaps halten die Textur so grob wie das Raster des Bildes, sonst flimmert sie, sobald ein Pixel mehrere Texel deckt.
// Die Welt rendert im Raster der PS1, die Stufe wählt die GPU deshalb von selbst richtig
uniform sampler2D albedo_texture : source_color, filter_nearest_mipmap, repeat_enable, hint_default_white;
uniform vec2 uv_scale = vec2(1.0);

// Größer als 0: Die Textur folgt der Welt statt dem Netz, der Wert ist die Zahl der Kacheln pro Meter
uniform float world_uv_scale = 0.0;

uniform vec3 tint = vec3(1.0);
uniform vec4 emission : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform float emission_energy = 0.0;

// Die PS1 kannte keine Bruchteile von Pixeln, jeder Eckpunkt rastet auf einem Raster ein. Es ist feiner als das Bild,
// weil die Figuren bei großen Pixeln sonst zu stark zappeln. Ps1Look setzt es, gezählt in Zellen seiner Stufe über das ganze Bild
uniform float snap = 1.0;
uniform vec2 snap_resolution = vec2(852.0, 480.0);

// Die PS1 verteilte Texturen gerade über den Bildschirm, ohne die Tiefe zu beachten
uniform float affine = 1.0;

varying vec3 affine_uv;

#ifdef LIFTED
// Spuren liegen flach auf Boden oder Mauer. Das Einrasten verschiebt ihre Eckpunkte anders als die der Fläche darunter,
// und wo die Fläche dadurch näher liegt, gewinnt sie den Tiefentest: Die Spur flackert, und die Fläche scheint durch.
// Deshalb rückt jede Spur längs des Blicks so weit zur Kamera, wie ein Pixel Versatz auf ihrer Fläche an Tiefe ausmacht.
// Im Bild bleibt sie, wo sie ist
uniform float lift = 0.02;

vec3 lifted(vec3 view, vec3 normal, mat4 projection) {
	// Der Betrag, weil Godot die Y-Achse der Projektion im Vulkan-Renderer spiegelt
	bool is_orthogonal = projection[3][3] == 1.0;
	float w = is_orthogonal ? 1.0 : abs(view.z);
	float pixel = 2.0 * w / (abs(projection[1][1]) * snap_resolution.y);
	vec3 n = normalize(normal);
	float slope = (abs(n.x) + abs(n.y)) / max(abs(n.z), 0.05);
	float amount = lift + snap * pixel * min(slope, 20.0);

	return is_orthogonal ? view + vec3(0.0, 0.0, amount) : view * max(0.0, 1.0 - amount / max(length(view), 0.001));
}
#endif

void vertex() {
	vec2 uv = UV * uv_scale;

	if (world_uv_scale > 0.0) {
		vec3 world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
		vec3 facing = abs(normalize(mat3(MODEL_MATRIX) * NORMAL));

		if (facing.y > 0.5) {
			uv = world.xz;
		} else if (facing.x > 0.5) {
			uv = vec2(world.z, -world.y);
		} else {
			uv = vec2(world.x, -world.y);
		}

		uv *= world_uv_scale;
	}

	vec4 view = MODELVIEW_MATRIX * vec4(VERTEX, 1.0);

#ifdef LIFTED
	view.xyz = lifted(view.xyz, MODELVIEW_NORMAL_MATRIX * NORMAL, PROJECTION_MATRIX);
#endif

	vec4 clip = PROJECTION_MATRIX * view;

	if (snap > 0.5) {
		vec2 grid = snap_resolution * 0.5;

		clip.xy = round(clip.xy / clip.w * grid) / grid * clip.w;
	}

	POSITION = clip;

	float depth = mix(1.0, clip.w, affine);

	affine_uv = vec3(uv * depth, depth);
}

const float BAYER[16] = float[](
	0.0, 8.0, 2.0, 10.0,
	12.0, 4.0, 14.0, 6.0,
	3.0, 11.0, 1.0, 9.0,
	15.0, 7.0, 13.0, 5.0
);

// Wahr, wo das Punktmuster an dieser Stelle des Bildes ein Loch lässt: 0 lässt nichts weg, 1 alles.
// pixel ist FRAGCOORD.xy, jeder Pixel des Bildes ist eine Zelle der PS1
bool is_cut(float amount, vec2 pixel) {
	if (amount <= 0.0) {
		return false;
	}

	vec2 cell = floor(pixel);
	int index = int(mod(cell.x, 4.0)) + int(mod(cell.y, 4.0)) * 4;

	return 1.0 - amount < (BAYER[index] + 0.5) / 16.0;
}

vec4 sample_texel(vec3 uv) {
	return texture(albedo_texture, uv.xy / uv.z);
}

vec3 sample_surface(vec3 uv) {
	return albedo.rgb * sample_texel(uv).rgb * tint;
}
