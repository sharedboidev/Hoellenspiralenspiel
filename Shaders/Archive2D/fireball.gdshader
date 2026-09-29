shader_type canvas_item;

uniform vec4 outline_color : source_color = vec4(1.0, 0.4, 0.1, 1.0);
uniform float outline_size : hint_range(1.0, 6.0) = 5.0;
uniform float intensity : hint_range(0.0, 2.0) = 1.1;

void fragment() {
    vec2 size = TEXTURE_PIXEL_SIZE * outline_size;
    vec4 sprite = texture(TEXTURE, UV);

    float alpha = 0.0;

    // Nachbar-Pixel abtasten
    alpha += texture(TEXTURE, UV + vec2( size.x, 0.0)).a;
    alpha += texture(TEXTURE, UV + vec2(-size.x, 0.0)).a;
    alpha += texture(TEXTURE, UV + vec2(0.0,  size.y)).a;
    alpha += texture(TEXTURE, UV + vec2(0.0, -size.y)).a;

    float outline = clamp(alpha, 0.0, 1.0) * (1.0 - sprite.a);

    vec3 final_color = sprite.rgb;
    final_color += outline_color.rgb * outline * intensity;

    COLOR = vec4(final_color, max(sprite.a, outline));
}
