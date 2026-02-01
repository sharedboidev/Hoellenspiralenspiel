shader_type canvas_item;

uniform float fill_amount : hint_range(0.0, 1.0) = 1.0;
uniform vec4 liquid_color : source_color = vec4(0.8, 0.0, 0.0, 1.0);
uniform float wave_strength = 0.02;
uniform float time_speed = 5;

void fragment() {
    // UV von 0..1 → -1..1
    vec2 uv = UV * 2.0 - 1.0;

    // Kreisform
    float dist = length(uv);
	
    if (dist > 1.0)
        discard;

    // Flüssigkeits-Welle
    float wave = sin(uv.x * 10.0 + TIME * time_speed) * wave_strength;

    // Füllhöhe
    float height = 1.0 - fill_amount * 2.0 + wave;

    if (uv.y < height)
        discard;
		
	// ---- Farbe hübscher machen ----
	float y01 = (uv.y + 1.0) * 0.5;                 // 0..1 (oben->unten)
	float shade = mix(0.55, 1.05, 1.0 - y01);        // oben heller
	vec3 col = liquid_color.rgb * shade;

	// Rand abdunkeln
	float edge = smoothstep(0.70, 1.00, dist);
	col *= mix(1.0, 0.55, edge);

	// kleines Highlight
	vec2 hlPos = vec2(-0.25, -0.35);
	float hl = 1.0 - smoothstep(0.0, 0.35, length(uv - hlPos));
	col += vec3(1.0) * hl * 0.25;

	COLOR = vec4(col, liquid_color.a);
}