using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Ein gerades Stück eines Risses auf dem Boden, in Pixeln
public readonly record struct FissureSegment(float AX, float AY, float BX, float BY)
{
    public float DistanceTo(float x, float y)
    {
        var dx     = BX - AX;
        var dy     = BY - AY;
        var length = dx * dx + dy * dy;
        var t      = length > 0f ? Math.Clamp(((x - AX) * dx + (y - AY) * dy) / length, 0f, 1f) : 0f;
        var px     = AX + t * dx - x;
        var py     = AY + t * dy - y;

        return MathF.Sqrt(px * px + py * py);
    }

    public bool Intersects(FissureSegment other)
    {
        var d1 = Cross(other.AX, other.AY, other.BX, other.BY, AX, AY);
        var d2 = Cross(other.AX, other.AY, other.BX, other.BY, BX, BY);
        var d3 = Cross(AX, AY, BX, BY, other.AX, other.AY);
        var d4 = Cross(AX, AY, BX, BY, other.BX, other.BY);

        return (d1 > 0f) != (d2 > 0f) && (d3 > 0f) != (d4 > 0f) && d1 != 0f && d2 != 0f && d3 != 0f && d4 != 0f;
    }

    public float DistanceTo(FissureSegment other)
    {
        if (Intersects(other))
            return 0f;

        return Math.Min(Math.Min(DistanceTo(other.AX, other.AY), DistanceTo(other.BX, other.BY)),
                        Math.Min(other.DistanceTo(AX, AY), other.DistanceTo(BX, BY)));
    }

    private static float Cross(float ax, float ay, float bx, float by, float px, float py)
        => (bx - ax) * (py - ay) - (by - ay) * (px - ax);
}

//Distance ist der Weg entlang des Risses ab seinem Anfang, Width der Anteil der vollen Breite
public readonly record struct FissurePoint(float X, float Y, float Distance, float Width);

//Wie ein Riss sich seinen Weg sucht. Ein Nebenriss weicht anderen Rissen aus, ein Hauptriss darf sie kreuzen (siehe FissurePath.Trace).
//Er geht in Schritten von StepLength, weicht je Schritt bis WanderDegrees von seiner Richtung ab und
//kehrt mit PullBack zur Hauptrichtung zurück. Kommt er einem anderen Riss näher als Clearance, probiert er Richtungen in Stufen von
//CandidateDegrees, höchstens MaxDeviationDegrees von der Hauptrichtung. Um seinen Anfang wächst der Abstand bis StartFreeRadius erst an,
//dort beginnen mehrere Risse am selben Punkt. Danach wird der Weg zur glatten Kurve und schlängelt sich quer dazu in zwei Lagen: weite, runde Bögen
//(Meander) und kleine, kantige Knicke (Detail), wie ein Riss in trockenem Boden. Die Breite schwillt in eigenen Bögen an und ab.
//Haarrisse zweigen nur zum Ansehen ab
public sealed record FissureStyle
{
    public static FissureStyle Default { get; } = new();

    public float StepLength          { get; init; } = 80f;
    public float WanderDegrees       { get; init; } = 34f;
    public float PullBack            { get; init; } = 0.22f;
    public float CandidateDegrees    { get; init; } = 15f;
    public int   CandidatesPerSide   { get; init; } = 6;
    public float MaxDeviationDegrees { get; init; } = 80f;
    public float Clearance           { get; init; } = 110f;
    public float StartFreeRadius     { get; init; } = 250f;

    //Nur für Risse, die andere kreuzen dürfen: Näher als ParallelRange an einem anderen Riss läuft keiner flacher als ParallelDegrees zu ihm.
    //Nach dem Kreuzen knickt er rechtwinklig vom gekreuzten Riss ab, um bis CrossTurnJitterDegrees schief
    public float ParallelRange          { get; init; } = 160f;
    public float ParallelDegrees        { get; init; } = 35f;
    public float CrossTurnJitterDegrees { get; init; } = 10f;

    //Abstand der Punkte auf der Kurve, Wellenlänge und Ausschlag der Bögen und Knicke, alles in Pixeln
    public float SampleSpacing     { get; init; } = 10f;
    public float MeanderWavelength { get; init; } = 120f;
    public float MeanderAmplitude  { get; init; } = 35f;
    public float DetailWavelength  { get; init; } = 28f;
    public float DetailAmplitude   { get; init; } = 6f;

    //Die Breite schwankt in Bögen dieser Länge zwischen den beiden Anteilen
    public float WidthWavelength { get; init; } = 70f;
    public float WidthMin        { get; init; } = 0.5f;
    public float WidthMax        { get; init; } = 1.35f;

    //So weit quer kann die Kurve höchstens vom groben Weg abweichen
    public float MaxLateral => MeanderAmplitude + DetailAmplitude;

    //Zur Spitze hin wird der Riss auf diesem Anteil seiner Länge schmal
    public float TipShare { get; init; } = 0.2f;
    public float TipWidth { get; init; } = 0.15f;

    //Auch der Anfang läuft schmal aus, auf so vielen Pixeln. Ein Nebenriss wächst so aus dem Boden, statt abgeschnitten zu beginnen
    public float StartTaperLength { get; init; } = 70f;
    public float StartWidth       { get; init; } = 0.3f;

    //Haarrisse je Schritt des Risses, ihre Länge in Pixeln und ihre Breite am Ansatz
    public float TwigChance    { get; init; } = 0.5f;
    public float TwigMinLength { get; init; } = 30f;
    public float TwigMaxLength { get; init; } = 110f;
    public float TwigWidth     { get; init; } = 0.4f;
}

//Der fertige Riss: die Punkte, auf denen er brennt, und die Haarrisse daneben. Alles in Pixeln
public sealed class FissureShape
{
    public FissureShape(IReadOnlyList<FissurePoint> points, IReadOnlyList<IReadOnlyList<FissurePoint>> twigs)
    {
        Points = points;
        Twigs  = twigs;
        Length = points.Count > 0 ? points[^1].Distance : 0f;

        if (points.Count == 0)
            return;

        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;

        foreach (var point in points)
        {
            minX = Math.Min(minX, point.X);
            minY = Math.Min(minY, point.Y);
            maxX = Math.Max(maxX, point.X);
            maxY = Math.Max(maxY, point.Y);
        }

        CenterX = (minX + maxX) / 2f;
        CenterY = (minY + maxY) / 2f;

        foreach (var point in points)
            ReachRadius = Math.Max(ReachRadius, MathF.Sqrt((point.X - CenterX) * (point.X - CenterX) + (point.Y - CenterY) * (point.Y - CenterY)));
    }

    public IReadOnlyList<FissurePoint>                Points { get; }
    public IReadOnlyList<IReadOnlyList<FissurePoint>> Twigs  { get; }

    public float Length { get; }

    //Ein Kreis um den ganzen Riss, für die Suche nach Einheiten
    public float CenterX     { get; }
    public float CenterY     { get; }
    public float ReachRadius { get; }

    public IEnumerable<FissureSegment> Segments
    {
        get
        {
            for (var i = 1; i < Points.Count; i++)
                yield return new FissureSegment(Points[i - 1].X, Points[i - 1].Y, Points[i].X, Points[i].Y);
        }
    }

    //Der Abstand zum Teil des Risses, der schon gewachsen ist
    public float DistanceTo(float x, float y, float grownShare = 1f)
    {
        if (Points.Count == 0)
            return float.MaxValue;

        var grown    = Length * Math.Clamp(grownShare, 0f, 1f);
        var distance = MathF.Sqrt((x - Points[0].X) * (x - Points[0].X) + (y - Points[0].Y) * (y - Points[0].Y));

        for (var i = 1; i < Points.Count; i++)
        {
            var from = Points[i - 1];
            var to   = Points[i];

            if (from.Distance >= grown)
                break;

            if (to.Distance > grown)
            {
                var t = (grown - from.Distance) / Math.Max(0.0001f, to.Distance - from.Distance);

                to = new FissurePoint(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t, grown, to.Width);
            }

            distance = Math.Min(distance, new FissureSegment(from.X, from.Y, to.X, to.Y).DistanceTo(x, y));
        }

        return distance;
    }

    //Eine Einheit steht auf dem Riss, wenn ihr Körper seine Breite berührt
    public bool Touches(float x, float y, float bodyRadius, float halfWidth, float grownShare = 1f)
        => DistanceTo(x, y, grownShare) <= Math.Max(0f, bodyRadius) + Math.Max(0f, halfWidth);
}

public static class FissurePath
{
    //Wahr, wenn zwischen den beiden Punkten eine Mauer steht
    public delegate bool BlockedCheck(float fromX, float fromY, float toX, float toY);

    private const float FullCircle = MathF.Tau;

    private const float CurveReserve = 1.3f;

    //Der Riss läuft von start ungefähr in Richtung angle (Bogenmaß, 0 zeigt nach +X), bis er length lang ist oder eine Mauer ihn aufhält.
    //Ohne crossesOthers weicht er den Rissen in obstacles aus, wo es geht, und schneidet sie nur, wenn keine Richtung frei ist.
    //Mit crossesOthers kreuzt er sie, läuft aber nicht neben ihnen her und knickt nach dem Kreuzen rechtwinklig ab. Sich selbst weicht er immer aus
    public static FissureShape Trace(float                         startX,
                                     float                         startY,
                                     float                         angle,
                                     float                         length,
                                     IReadOnlyList<FissureSegment> obstacles,
                                     IRandomSource                 random,
                                     BlockedCheck                  isBlocked     = null,
                                     FissureStyle                  style         = null,
                                     bool                          crossesOthers = false)
    {
        ArgumentNullException.ThrowIfNull(random);

        style     ??= FissureStyle.Default;
        obstacles ??= Array.Empty<FissureSegment>();

        //Die Kurve schneidet Ecken ab und wird so kürzer als der grobe Weg, darum geht der etwas weiter. Refine schneidet bei length ab
        var coarse = Walk(startX, startY, angle, Math.Max(0f, length) * CurveReserve, obstacles, random, isBlocked, style, crossesOthers);
        var points = Refine(coarse, Math.Max(0f, length), random, style);

        return new FissureShape(points, GrowTwigs(points, random, isBlocked, style));
    }

    //Die Richtung, in der vom Punkt aus am meisten Platz zu anderen Rissen ist. Bei gleich viel Platz entscheidet der Zufall.
    //Geprüft wird ein Stück von probeLength ab dem Rand von StartFreeRadius, so zählt der Riss nicht, auf dem der Punkt liegt
    public static float PickFreeAngle(float                         x,
                                      float                         y,
                                      IReadOnlyList<FissureSegment> obstacles,
                                      IRandomSource                 random,
                                      BlockedCheck                  isBlocked   = null,
                                      FissureStyle                  style       = null,
                                      int                           candidates  = 12,
                                      float                         probeLength = 300f)
    {
        ArgumentNullException.ThrowIfNull(random);

        style      ??= FissureStyle.Default;
        obstacles  ??= Array.Empty<FissureSegment>();
        candidates =   Math.Max(1, candidates);

        var offset    = random.NextFloat() * FullCircle;
        var bestAngle = offset;
        var bestRoom  = float.MinValue;

        for (var i = 0; i < candidates; i++)
        {
            var angle = offset + FullCircle * i / candidates;
            var cos   = MathF.Cos(angle);
            var sin   = MathF.Sin(angle);
            var near  = style.StartFreeRadius;
            var far   = near + probeLength;
            var probe = new FissureSegment(x + cos * near, y + sin * near, x + cos * far, y + sin * far);

            if (isBlocked is not null && isBlocked(x, y, probe.BX, probe.BY))
                continue;

            //Mehr Platz als die Probe lang ist, zählt nicht mehr, dann entscheidet die Reihenfolge, die schon zufällig beginnt
            var room = probeLength;

            foreach (var obstacle in obstacles)
                room = Math.Min(room, probe.DistanceTo(obstacle));

            if (room <= bestRoom)
                continue;

            bestRoom  = room;
            bestAngle = angle;
        }

        return bestAngle;
    }

    private static List<(float X, float Y)> Walk(float                         startX,
                                                 float                         startY,
                                                 float                         mainAngle,
                                                 float                         length,
                                                 IReadOnlyList<FissureSegment> obstacles,
                                                 IRandomSource                 random,
                                                 BlockedCheck                  isBlocked,
                                                 FissureStyle                  style,
                                                 bool                          crossesOthers)
    {
        var points       = new List<(float X, float Y)> { (startX, startY) };
        var own          = new List<FissureSegment>();
        var heading      = mainAngle;
        var travelled    = 0f;
        var stepLength   = Math.Max(1f, style.StepLength);
        var candidateRad = DegToRad(style.CandidateDegrees);
        var maxDeviation = DegToRad(style.MaxDeviationDegrees);

        while (travelled < length - 0.01f)
        {
            var step   = Math.Min(stepLength, length - travelled);
            var wander = heading + random.NextRange(-1f, 1f) * DegToRad(style.WanderDegrees);
            var target = wander + AngleDifference(mainAngle, wander) * style.PullBack;
            var (x, y) = points[^1];

            var chosen       = float.NaN;
            var fallback     = float.NaN;
            var fallbackRoom = float.MinValue;
            var firstSign    = random.NextFloat() < 0.5f ? 1 : -1;

            for (var k = 0; k <= style.CandidatesPerSide * 2 && float.IsNaN(chosen); k++)
            {
                //0, dann abwechselnd zu beiden Seiten weiter weg
                var ring      = (k + 1) / 2;
                var sign      = k % 2 == 1 ? firstSign : -firstSign;
                var candidate = target + sign * ring * candidateRad;

                if (Math.Abs(AngleDifference(candidate, mainAngle)) > maxDeviation)
                    continue;

                var toX = x + MathF.Cos(candidate) * step;
                var toY = y + MathF.Sin(candidate) * step;

                if (isBlocked is not null && isBlocked(x, y, toX, toY))
                    continue;

                //Wer kreuzen darf, hält nur Abstand zu sich selbst und meidet das Nebeneinander
                var stepSegment = new FissureSegment(x, y, toX, toY);
                var room        = GetRoom(stepSegment, obstacles, crossesOthers ? 0 : obstacles.Count, own, own.Count - 1);
                var isFree      = room >= GetClearance(toX - startX, toY - startY, style);

                if (isFree && crossesOthers)
                    isFree = !RunsAlongside(stepSegment, candidate, obstacles, startX, startY, style);

                if (isFree)
                    chosen = candidate;
                else if (room > fallbackRoom)
                {
                    fallbackRoom = room;
                    fallback     = candidate;
                }
            }

            var next = float.IsNaN(chosen) ? fallback : chosen;

            //Mauern ringsum: Der Riss endet hier
            if (float.IsNaN(next))
                break;

            var end = (x + MathF.Cos(next) * step, y + MathF.Sin(next) * step);

            own.Add(new FissureSegment(x, y, end.Item1, end.Item2));
            points.Add(end);

            heading   =  next;
            travelled += step;

            //Hat er einen anderen Riss gekreuzt, läuft er von ihm aus rechtwinklig weiter, auf der Seite, auf der er jetzt ist
            if (crossesOthers && FindCrossedAxis(own[^1], obstacles, startX, startY, style) is { } axis)
            {
                var across = axis + MathF.PI / 2f;

                if (MathF.Cos(across - next) < 0f)
                    across += MathF.PI;

                mainAngle = across + random.NextRange(-1f, 1f) * DegToRad(style.CrossTurnJitterDegrees);
                heading   = mainAngle;
            }
        }

        return points;
    }

    //Wahr, wenn der Schritt nah an einem anderen Riss flacher als ParallelDegrees zu ihm läuft. Um den Start gilt das nicht,
    //dort beginnen die Risse eines Einschlags nebeneinander
    private static bool RunsAlongside(FissureSegment step, float angle, IReadOnlyList<FissureSegment> obstacles, float startX, float startY, FissureStyle style)
    {
        if (GetStartShare(Distance((step.BX, step.BY), (startX, startY)), style) < 1f)
            return false;

        if (GetAxis(obstacles, step.BX, step.BY, style.ParallelRange) is not { } axis)
            return false;

        var difference = Math.Abs(AngleDifference(angle, axis)) % MathF.PI;

        return Math.Min(difference, MathF.PI - difference) < DegToRad(style.ParallelDegrees);
    }

    //Die Richtung des Risses, den der Schritt kreuzt, gemessen um den Schnittpunkt. Null, wenn er keinen kreuzt oder nur am Start
    private static float? FindCrossedAxis(FissureSegment step, IReadOnlyList<FissureSegment> obstacles, float startX, float startY, FissureStyle style)
    {
        var nearest = float.MaxValue;
        var hit     = (X: 0f, Y: 0f);

        foreach (var obstacle in obstacles)
        {
            if (!step.Intersects(obstacle))
                continue;

            //Wo er die Linie des anderen kreuzt, ungefähr: der Punkt des anderen, der dem Schritt am nächsten liegt
            var middle = ((obstacle.AX + obstacle.BX) / 2f, (obstacle.AY + obstacle.BY) / 2f);
            var along  = Distance((step.AX, step.AY), middle);

            if (along >= nearest || GetStartShare(Distance(middle, (startX, startY)), style) < 1f)
                continue;

            nearest = along;
            hit     = middle;
        }

        return nearest < float.MaxValue ? GetAxis(obstacles, hit.X, hit.Y, style.StepLength) : null;
    }

    //Die mittlere Richtung aller Stücke anderer Risse im Umkreis, ohne Vorzeichen: 10° und 190° sind dieselbe Achse.
    //Die Zacken eines einzelnen Stücks heben sich so auf
    private static float? GetAxis(IReadOnlyList<FissureSegment> obstacles, float x, float y, float radius)
    {
        var sumX = 0f;
        var sumY = 0f;

        foreach (var obstacle in obstacles)
        {
            if (obstacle.DistanceTo(x, y) > radius)
                continue;

            var dx     = obstacle.BX - obstacle.AX;
            var dy     = obstacle.BY - obstacle.AY;
            var length = MathF.Sqrt(dx * dx + dy * dy);

            if (length <= 0f)
                continue;

            //Doppelter Winkel, gewichtet mit der Länge
            var doubled = 2f * MathF.Atan2(dy, dx);

            sumX += MathF.Cos(doubled) * length;
            sumY += MathF.Sin(doubled) * length;
        }

        return sumX * sumX + sumY * sumY > 0.0001f ? MathF.Atan2(sumY, sumX) / 2f : null;
    }

    //Der kleinste Abstand des hinteren Teils des Schritts zu einem fremden Riss oder zu einem eigenen Stück vor dem letzten.
    //Schneidet der Schritt eins davon, ist kein Platz
    private static float GetRoom(FissureSegment step, IReadOnlyList<FissureSegment> obstacles, int obstacleCount, List<FissureSegment> own, int ownCount)
    {
        var tail = new FissureSegment((step.AX + step.BX) / 2f, (step.AY + step.BY) / 2f, step.BX, step.BY);
        var room = float.MaxValue;

        for (var i = 0; i < obstacleCount; i++)
            room = Math.Min(room, step.Intersects(obstacles[i]) ? 0f : tail.DistanceTo(obstacles[i]));

        for (var i = 0; i < ownCount; i++)
            room = Math.Min(room, step.Intersects(own[i]) ? 0f : tail.DistanceTo(own[i]));

        return room;
    }

    //Am Start beginnen mehrere Risse am selben Punkt, ein Nebenriss sogar auf einem anderen Riss. Der nötige Abstand wächst darum
    //vom Start bis StartFreeRadius auf Clearance an, im selben Maß wie das Schlängeln in Refine
    private static float GetClearance(float offsetX, float offsetY, FissureStyle style)
        => style.Clearance * GetStartShare(MathF.Sqrt(offsetX * offsetX + offsetY * offsetY), style);

    private static float GetStartShare(float distance, FissureStyle style)
        => Math.Min(1f, distance / Math.Max(1f, style.StartFreeRadius));

    //Der grobe Weg wird zur glatten Kurve und quer dazu mit Bögen und Knicken versetzt. Am Anfang setzt der Versatz bis StartFreeRadius
    //sanft ein, so beginnt der Riss genau am Start und kommt dem Nachbarn aus demselben Einschlag nicht zu nah. Er endet, wo er maxLength erreicht. Die Breite schwillt an und ab und läuft zu beiden Enden schmal aus
    private static List<FissurePoint> Refine(List<(float X, float Y)> coarse, float maxLength, IRandomSource random, FissureStyle style)
    {
        var curve   = SampleCurve(coarse, Math.Max(1f, style.SampleSpacing));
        var meander = new Wave(random, style.MeanderWavelength, true);
        var detail  = new Wave(random, style.DetailWavelength, false);
        var swell   = new Wave(random, style.WidthWavelength, true);
        var fine    = new List<(float X, float Y)>(curve.Count);
        var along   = 0f;

        for (var i = 0; i < curve.Count; i++)
        {
            if (i > 0)
                along += Distance(curve[i - 1], curve[i]);

            var before  = curve[Math.Max(0, i - 1)];
            var after   = curve[Math.Min(curve.Count - 1, i + 1)];
            var dx      = after.X - before.X;
            var dy      = after.Y - before.Y;
            var span    = MathF.Sqrt(dx * dx + dy * dy);
            var easeIn  = GetStartShare(along, style);
            var lateral = (meander.At(along) * style.MeanderAmplitude + detail.At(along) * style.DetailAmplitude) * easeIn;

            fine.Add(span > 0f ? (curve[i].X - dy / span * lateral, curve[i].Y + dx / span * lateral) : curve[i]);
        }

        var points   = new List<FissurePoint>(fine.Count);
        var distance = 0f;

        for (var i = 0; i < fine.Count; i++)
        {
            var point = fine[i];

            if (i > 0)
            {
                var step = Distance(fine[i - 1], point);

                if (distance + step > maxLength)
                {
                    var t = (maxLength - distance) / step;

                    point    = (fine[i - 1].X + (point.X - fine[i - 1].X) * t, fine[i - 1].Y + (point.Y - fine[i - 1].Y) * t);
                    distance = maxLength;

                    points.Add(new FissurePoint(point.X, point.Y, distance, 0f));

                    break;
                }

                distance += step;
            }

            points.Add(new FissurePoint(point.X, point.Y, distance, 0f));
        }

        var length  = distance;
        var tipFrom = length * (1f - Math.Clamp(style.TipShare, 0f, 1f));

        for (var i = 0; i < points.Count; i++)
        {
            var point = points[i];
            var width = style.WidthMin + (style.WidthMax - style.WidthMin) * (swell.At(point.Distance) + 1f) / 2f;

            if (point.Distance > tipFrom && length > tipFrom)
                width *= 1f - (1f - style.TipWidth) * (point.Distance - tipFrom) / (length - tipFrom);

            if (point.Distance < style.StartTaperLength)
                width *= style.StartWidth + (1f - style.StartWidth) * point.Distance / style.StartTaperLength;

            points[i] = point with { Width = width };
        }

        return points;
    }

    //Eine Catmull-Rom-Kurve durch alle Punkte des groben Wegs, etwa alle spacing Pixel ein Punkt
    private static List<(float X, float Y)> SampleCurve(List<(float X, float Y)> coarse, float spacing)
    {
        var curve = new List<(float X, float Y)> { coarse[0] };

        for (var i = 1; i < coarse.Count; i++)
        {
            var p0    = coarse[Math.Max(0, i - 2)];
            var p1    = coarse[i - 1];
            var p2    = coarse[i];
            var p3    = coarse[Math.Min(coarse.Count - 1, i + 1)];
            var steps = Math.Max(1, (int)MathF.Ceiling(Distance(p1, p2) / spacing));

            for (var step = 1; step <= steps; step++)
            {
                var t  = (float)step / steps;
                var t2 = t * t;
                var t3 = t2 * t;

                curve.Add((0.5f * (2f * p1.X + (p2.X - p0.X) * t + (2f * p0.X - 5f * p1.X + 4f * p2.X - p3.X) * t2 + (3f * p1.X - p0.X - 3f * p2.X + p3.X) * t3),
                           0.5f * (2f * p1.Y + (p2.Y - p0.Y) * t + (2f * p0.Y - 5f * p1.Y + 4f * p2.Y - p3.Y) * t2 + (3f * p1.Y - p0.Y - 3f * p2.Y + p3.Y) * t3)));
            }
        }

        return curve;
    }

    //Zufällige Werte von -1 bis 1 im Abstand einer Wellenlänge, dazwischen weich (Bögen) oder gerade (Knicke) verbunden
    private sealed class Wave(IRandomSource random, float wavelength, bool isSmooth)
    {
        private readonly List<float> values = new();

        public float At(float distance)
        {
            var position = Math.Max(0f, distance) / Math.Max(1f, wavelength);
            var cell     = (int)position;
            var t        = position - cell;

            while (values.Count <= cell + 1)
                values.Add(random.NextRange(-1f, 1f));

            if (isSmooth)
                t = t * t * (3f - 2f * t);

            return values[cell] + (values[cell + 1] - values[cell]) * t;
        }
    }

    //Kurze Haarrisse schräg vom Riss weg, schmal und ohne Schaden. Sie wachsen mit, darum zählt ihr Weg ab dem Anfang des Risses
    private static List<IReadOnlyList<FissurePoint>> GrowTwigs(List<FissurePoint> points, IRandomSource random, BlockedCheck isBlocked, FissureStyle style)
    {
        var twigs   = new List<IReadOnlyList<FissurePoint>>();
        var perFine = style.TwigChance * Math.Max(1f, style.SampleSpacing) / Math.Max(1f, style.StepLength);

        for (var i = 1; i < points.Count - 1; i++)
        {
            if (random.NextFloat() >= perFine)
                continue;

            var root    = points[i];
            var tangent = MathF.Atan2(points[i + 1].Y - points[i - 1].Y, points[i + 1].X - points[i - 1].X);
            var side    = random.NextFloat() < 0.5f ? -1f : 1f;
            var angle   = tangent + side * DegToRad(random.NextRange(35f, 75f));
            var length  = random.NextRange(style.TwigMinLength, style.TwigMaxLength);
            var pieces  = length > (style.TwigMinLength + style.TwigMaxLength) / 2f ? 3 : 2;
            var twig    = new List<FissurePoint> { root with { Width = root.Width * style.TwigWidth } };

            for (var piece = 1; piece <= pieces; piece++)
            {
                var last    = twig[^1];
                var section = length / pieces;

                angle += random.NextRange(-1f, 1f) * DegToRad(25f);

                var x = last.X + MathF.Cos(angle) * section;
                var y = last.Y + MathF.Sin(angle) * section;

                if (isBlocked is not null && isBlocked(last.X, last.Y, x, y))
                    break;

                var share = 1f - (float)piece / pieces;

                twig.Add(new FissurePoint(x, y, last.Distance + section, style.TwigWidth * (0.25f + 0.75f * share) * root.Width));
            }

            if (twig.Count > 1)
                twigs.Add(twig);
        }

        return twigs;
    }

    private static float Distance((float X, float Y) a, (float X, float Y) b)
        => MathF.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));

    private static float DegToRad(float degrees)
        => degrees * MathF.PI / 180f;

    //Von from nach to auf dem kürzeren Weg, zwischen -Pi und Pi
    private static float AngleDifference(float to, float from)
    {
        var difference = (to - from) % FullCircle;

        if (difference > MathF.PI)
            difference -= FullCircle;
        else if (difference < -MathF.PI)
            difference += FullCircle;

        return difference;
    }
}
