shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, shadows_disabled, fog_disabled;

uniform vec4 tint : source_color = vec4(1.0);
uniform float brightness = 1.0;

// Kleiner als 1 zieht die Seiten des Sterns nach innen, je kleiner, desto schlanker die vier Zacken
uniform float slimness = 0.7;

// So viele Sekunden dauert ein Atemzug
uniform float pulse_sec = 2.6;
// Um diesen Anteil wächst und schrumpft der Stern
uniform float pulse_size = 0.15;
// Um diesen Anteil lässt das Glimmen nach
uniform float pulse_glow = 0.35;

// So weit rückt der Stern auf die Kamera zu, damit sein Träger ihn nicht verdeckt
uniform float toward_camera = 0.4;
// Versatz auf dem Bildschirm in Metern, nach rechts und nach oben
uniform vec2 view_offset = vec2(0.0);

varying float glow;

void vertex() {
	vec3 origin = MODEL_MATRIX[3].xyz;

	// Der Ort verschiebt den Takt, sonst atmeten alle Sterne im Gleichschritt
	float breath = sin(TIME * TAU / pulse_sec + origin.x * 1.7 + origin.z * 2.3);

	glow = 1.0 - pulse_glow * (0.5 - 0.5 * breath);

	vec4 center = VIEW_MATRIX * vec4(origin, 1.0);

	center.xy += view_offset;
	center.z += toward_camera;

	vec2 corner = VERTEX.xy * length(MODEL_MATRIX[0].xyz) * (1.0 + pulse_size * breath);

	POSITION = PROJECTION_MATRIX * (center + vec4(corner, 0.0, 0.0));
}

void fragment() {
	vec2 from_center = abs(UV * 2.0 - 1.0);
	float inside = 1.0 - pow(from_center.x, slimness) - pow(from_center.y, slimness);

	ALBEDO = tint.rgb * brightness * glow;
	ALPHA = smoothstep(0.0, 0.2, inside) * tint.a * glow;
}
