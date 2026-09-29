// Held, Gegner und was sie tragen. Sie markieren sich im Rauheitskanal, daran findet ps1_unit_outline ihre Kanten.
// Ohne Glanzlicht spielt die Rauheit für das Aussehen keine Rolle. Die Umgebung hat 1, der leere Hintergrund 0
const float UNIT_MARK = 0.4;
const float HIGHLIGHT_MARK = 0.7;
const float NO_MARK = 1.0;

// Wer weiter vom Helden entfernt steht, als er sieht, bleibt verborgen. Am Rand blendet er mit dem Punktmuster der Mauern ein.
// Dieselbe Rechnung steht in SightRange. Ein Radius von 0 hebt die Grenze auf
uniform vec3 sight_center = vec3(0.0);
uniform float sight_radius = 0.0;
uniform float sight_edge = 1.5;

// 1 für den Gegner unter der Maus
instance uniform float highlight = 0.0;

// Aus für Teile, die zur Einheit gehören, aber keinen Umriss tragen, etwa die Aura am Boden
uniform bool outlined = true;

#ifdef CUTOUT
// Der Alphakanal der Textur schneidet Löcher ins Netz, etwa zwischen die Rippen
uniform float alpha_cutoff : hint_range(0.0, 1.0) = 0.5;
#endif

void fragment() {
	float hidden = 0.0;

	if (sight_radius > 0.0) {
		vec3 world = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;

		hidden = smoothstep(sight_radius - max(sight_edge, 0.0001), sight_radius, distance(world.xz, sight_center.xz));

		if (is_cut(hidden, SCREEN_UV)) {
			discard;
		}
	}

	vec4 texel = sample_texel(affine_uv);

#ifdef CUTOUT
	ALPHA = texel.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_cutoff;
#endif

	ALBEDO = albedo.rgb * texel.rgb * tint;
	EMISSION = emission.rgb * emission_energy;

	// Beim Einblenden bekäme jedes Loch im Punktmuster einen eigenen Rand. Der Umriss kommt erst, wenn die Figur ganz zu sehen ist
	ROUGHNESS = !outlined || hidden > 0.0 ? NO_MARK : (highlight > 0.5 ? HIGHLIGHT_MARK : UNIT_MARK);
	METALLIC = 0.0;
	SPECULAR = 0.0;
}
