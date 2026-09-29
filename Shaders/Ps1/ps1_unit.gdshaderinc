// Held, Gegner und was sie tragen. Sie markieren sich im Rauheitskanal, daran findet ps1_unit_outline ihre Kanten.
// Ohne Glanzlicht spielt die Rauheit für das Aussehen keine Rolle. Die Umgebung hat 1, der leere Hintergrund 0
const float UNIT_MARK = 0.4;
const float HIGHLIGHT_MARK = 0.7;

// 1 für den Gegner unter der Maus
instance uniform float highlight = 0.0;

#ifdef CUTOUT
// Der Alphakanal der Textur schneidet Löcher ins Netz, etwa zwischen die Rippen
uniform float alpha_cutoff : hint_range(0.0, 1.0) = 0.5;
#endif

void fragment() {
	vec4 texel = sample_texel(affine_uv);

#ifdef CUTOUT
	ALPHA = texel.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_cutoff;
#endif

	ALBEDO = albedo.rgb * texel.rgb * tint;
	EMISSION = emission.rgb * emission_energy;
	ROUGHNESS = highlight > 0.5 ? HIGHLIGHT_MARK : UNIT_MARK;
	METALLIC = 0.0;
	SPECULAR = 0.0;
}
