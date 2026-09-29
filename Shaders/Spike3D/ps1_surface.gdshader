shader_type spatial;
render_mode blend_mix, cull_back, diffuse_lambert, specular_disabled;

uniform vec4 albedo : source_color = vec4(1.0);
uniform sampler2D albedo_texture : source_color, filter_nearest, repeat_enable, hint_default_white;
uniform vec2 uv_scale = vec2(1.0);

// Größer als 0: Die Textur folgt der Welt statt dem Netz, der Wert ist die Zahl der Kacheln pro Meter
uniform float world_uv_scale = 0.0;

uniform vec3 tint = vec3(1.0);
uniform vec4 emission : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform float emission_energy = 0.0;

// Die PS1 kannte keine Bruchteile von Pixeln, jeder Eckpunkt rastet auf dem Raster der Auflösung ein
uniform float snap = 1.0;
uniform vec2 snap_resolution = vec2(426.0, 240.0);

// Die PS1 verteilte Texturen gerade über den Bildschirm, ohne die Tiefe zu beachten
uniform float affine = 1.0;

varying vec3 affine_uv;

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

	vec4 clip = PROJECTION_MATRIX * (MODELVIEW_MATRIX * vec4(VERTEX, 1.0));

	if (snap > 0.5) {
		vec2 grid = snap_resolution * 0.5;

		clip.xy = round(clip.xy / clip.w * grid) / grid * clip.w;
	}

	POSITION = clip;

	float depth = mix(1.0, clip.w, affine);

	affine_uv = vec3(uv * depth, depth);
}

void fragment() {
	vec4 texel = texture(albedo_texture, affine_uv.xy / affine_uv.z);

	ALBEDO = albedo.rgb * texel.rgb * tint;
	ROUGHNESS = 1.0;
	METALLIC = 0.0;
	EMISSION = emission.rgb * emission_energy;
}
