shader_type canvas_item;

uniform sampler2D screen : hint_screen_texture, filter_nearest, repeat_disable;

uniform vec2 resolution = vec2(426.0, 240.0);

// 32 Stufen pro Kanal sind die 15 Bit Farbtiefe der PS1
uniform float color_levels = 32.0;
uniform float dither = 1.0;

const float BAYER[16] = float[](
	0.0, 8.0, 2.0, 10.0,
	12.0, 4.0, 14.0, 6.0,
	3.0, 11.0, 1.0, 9.0,
	15.0, 7.0, 13.0, 5.0
);

void fragment() {
	vec2 cell = floor(SCREEN_UV * resolution);
	vec3 color = texture(screen, (cell + 0.5) / resolution).rgb;

	int index = int(mod(cell.x, 4.0)) + int(mod(cell.y, 4.0)) * 4;
	float threshold = (BAYER[index] + 0.5) / 16.0 - 0.5;
	float steps = max(color_levels - 1.0, 1.0);

	color = floor(color * steps + 0.5 + threshold * dither) / steps;

	COLOR = vec4(clamp(color, 0.0, 1.0), 1.0);
}
