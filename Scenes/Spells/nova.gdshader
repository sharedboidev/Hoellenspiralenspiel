shader_type canvas_item;
render_mode blend_add;

uniform float inner_radius = 0.0;
uniform float outer_radius = 100.0;
uniform float max_radius = 300.0;

uniform vec4 core_color : source_color = vec4(0.85, 0.95, 1.0, 1.0);
uniform vec4 ring_color : source_color = vec4(0.4, 0.7, 1.0, 1.0);

uniform sampler2D noise_tex : repeat_enable;
uniform float distortion_strength = 6.0;
uniform float edge_softness = 4.0;
uniform float time_scale = 0.8;

void fragment()
{
    vec2 center = vec2(0.5);
    float dist = distance(UV, center) * 512.0;

    // Animated noise for icy edge distortion
    vec2 noise_uv = UV * 4.0 + vec2(TIME * time_scale);
    float noise = texture(noise_tex, noise_uv).r;

    float distorted_outer = outer_radius + noise * distortion_strength;

    // Crisp ring
    float outer_edge = smoothstep(distorted_outer - edge_softness, distorted_outer, dist);
    float inner_edge = smoothstep(inner_radius, inner_radius + edge_softness, dist);

    float ring = inner_edge * (1.0 - outer_edge);

    // Core highlight line
    float core = smoothstep(outer_radius - 2.0, outer_radius, dist);

    vec3 final_color = mix(ring_color.rgb, core_color.rgb, core);

    // Fade when reaching max radius
    float life_fade = 1.0 - (outer_radius / max_radius);

    COLOR = vec4(final_color, ring * life_fade);
}
	