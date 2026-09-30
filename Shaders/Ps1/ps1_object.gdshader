shader_type spatial;
render_mode blend_mix, cull_back, diffuse_lambert, specular_disabled;

// Was der Held benutzen kann: Truhe, Händler, Durchgänge und Beutel. Sieht aus wie ps1_surface und trägt einen Umriss wie die Figuren,
// den ps1_unit_outline zieht

#include "res://Shaders/Ps1/ps1_common.gdshaderinc"
#include "res://Shaders/Ps1/ps1_marks.gdshaderinc"

// 0 kein Rand, 1 ein dunkler, 2 ein leuchtender in einer hellen Fassung von glow_hue. Gesetzt von ObjectOutline
instance uniform int outline = 1;
instance uniform float glow_hue = 0.0;

void fragment() {
	ALBEDO = sample_surface(affine_uv);
	ROUGHNESS = outline == 2 ? glow_mark(glow_hue) : (outline == 1 ? OBJECT_MARK : NO_MARK);
	METALLIC = 0.0;
	EMISSION = emission.rgb * emission_energy;
}
