// Figuren und was der Held benutzen kann, markieren sich im Rauheitskanal, daran findet ps1_unit_outline ihre Kanten.
// Ohne Glanzlicht spielt die Rauheit für das Aussehen keine Rolle. Die Umgebung hat 1, der leere Hintergrund 0.
// Der Kanal hält 8 Bit und kommt so an, wie er geschrieben wurde
const float UNIT_MARK = 0.4;
const float HIGHLIGHT_MARK = 0.7;
const float OBJECT_MARK = 0.3;
const float NO_MARK = 1.0;

// Was leuchtet, schreibt seinen Farbton in diesen Bereich. Das reicht für Schritte von etwa 7 Grad
const float GLOW_MARK_LOW = 0.04;
const float GLOW_MARK_HIGH = 0.24;

// Die Arten nach Rang. Grenzen zwei Arten an eine Stelle, zieht die höhere den Rand
const int MARK_NONE = 0;
const int MARK_GLOW = 1;
const int MARK_OBJECT = 2;
const int MARK_UNIT = 3;
const int MARK_HIGHLIGHT = 4;

// Der Farbton läuft von 0 bis 1 einmal um den Farbkreis
float glow_mark(float hue) {
	return mix(GLOW_MARK_LOW, GLOW_MARK_HIGH, fract(hue));
}

float mark_hue(float roughness) {
	return clamp((roughness - GLOW_MARK_LOW) / (GLOW_MARK_HIGH - GLOW_MARK_LOW), 0.0, 1.0);
}

int mark_kind(float roughness) {
	if (roughness < GLOW_MARK_LOW * 0.5 || roughness > 0.85) {
		return MARK_NONE;
	}

	if (roughness < 0.27) {
		return MARK_GLOW;
	}

	if (roughness < 0.35) {
		return MARK_OBJECT;
	}

	return roughness < 0.55 ? MARK_UNIT : MARK_HIGHLIGHT;
}
