using System;
using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

//Der Wunsch des Users vom 08.10.2026: Der Riss breitet sich in Blickrichtung aus, aber in einem wild wirkenden Muster wie brechende Platten,
//10 m lang. Alle Risse meiden einander, wo es geht, und schneiden sich nur, wenn es keine andere Möglichkeit gibt
[TestFixture]
public class FissurePathTests
{
    private const float Length = 1000f;
    private const int   Seeds  = 25;

    private static readonly FissureStyle Style = FissureStyle.Default;

    //Wie weit die Kurve mit Bögen und Knicken höchstens vom groben Weg abweicht, dazu etwas Luft für das Runden der Kurve
    private static float RefineSlack => Style.MaxLateral + 10f;

    private static FissureShape Trace(int seed, float angle = 0f, IReadOnlyList<FissureSegment> obstacles = null, FissurePath.BlockedCheck isBlocked = null)
        => FissurePath.Trace(0f, 0f, angle, Length, obstacles ?? Array.Empty<FissureSegment>(), new SeededRandom(seed), isBlocked);

    private static float DistanceFromStart(FissurePoint point)
        => MathF.Sqrt(point.X * point.X + point.Y * point.Y);

    [Test]
    public void Segment_AbstandUndSchnitt()
    {
        var segment = new FissureSegment(0f, 0f, 100f, 0f);

        Assert.Multiple(() =>
        {
            Assert.That(segment.DistanceTo(50f, 30f), Is.EqualTo(30f).Within(0.001f));
            Assert.That(segment.DistanceTo(-30f, -40f), Is.EqualTo(50f).Within(0.001f));
            Assert.That(segment.Intersects(new FissureSegment(50f, -10f, 50f, 10f)), Is.True);
            Assert.That(segment.Intersects(new FissureSegment(150f, -10f, 150f, 10f)), Is.False);
            Assert.That(segment.Intersects(new FissureSegment(100f, 0f, 150f, 50f)), Is.False, "ein gemeinsamer Endpunkt schneidet nicht");
            Assert.That(segment.DistanceTo(new FissureSegment(50f, -10f, 50f, 10f)), Is.Zero);
            Assert.That(segment.DistanceTo(new FissureSegment(0f, 20f, 100f, 20f)), Is.EqualTo(20f).Within(0.001f));
        });
    }

    [Test]
    public void Riss_IstMitDemselbenSeedGleich()
    {
        var a = Trace(3);
        var b = Trace(3);
        var c = Trace(4);

        Assert.Multiple(() =>
        {
            Assert.That(a.Points, Is.EqualTo(b.Points));
            Assert.That(a.Points, Is.Not.EqualTo(c.Points));
        });
    }

    [Test]
    public void Riss_Laeuft10MeterInBlickrichtung_GezacktUndNichtGerade()
    {
        for (var seed = 0; seed < Seeds; seed++)
        {
            var shape = Trace(seed);
            var end   = shape.Points[^1];

            Assert.Multiple(() =>
            {
                Assert.That(shape.Length, Is.EqualTo(Length).Within(0.01f), $"Seed {seed}: samt Zacken genau 10 m");
                Assert.That(end.X, Is.GreaterThan(Length * 0.5f), $"Seed {seed}: er kommt in Blickrichtung voran");
                Assert.That(shape.Points.All(point => point.X > -RefineSlack), Is.True, $"Seed {seed}: er läuft nicht zurück");
                Assert.That(shape.Points.Count, Is.GreaterThan(Length / Style.StepLength * 2), $"Seed {seed}: die Schritte sind verfeinert");
                Assert.That(shape.Points.Max(point => Math.Abs(point.Y)), Is.GreaterThan(20f), $"Seed {seed}: er ist keine gerade Linie");
            });
        }
    }

    [Test]
    public void Riss_ZaehltSeinenWeg_UndWirdZuBeidenEndenSchmal()
    {
        var shape = Trace(5);
        var tip   = shape.Points[^1];

        Assert.Multiple(() =>
        {
            Assert.That(shape.Points[0].Distance, Is.Zero);
            Assert.That(shape.Points.Zip(shape.Points.Skip(1)).All(pair => pair.Second.Distance > pair.First.Distance), Is.True);
            Assert.That(tip.Width, Is.LessThan(shape.Points.Average(point => point.Width) / 2f));
            Assert.That(shape.Points[0].Width, Is.LessThan(shape.Points.Average(point => point.Width) / 2f), "auch der Anfang ist schmal");
            Assert.That(shape.Points.All(point => point.Width > 0f), Is.True);
        });
    }

    [Test]
    public void Haarrisse_ZweigenSchmalAb_UndWachsenMit()
    {
        var twigs = Enumerable.Range(0, Seeds).SelectMany(seed => Trace(seed).Twigs).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(twigs, Is.Not.Empty);
            Assert.That(twigs.All(twig => twig.Count >= 2), Is.True);
            Assert.That(twigs.All(twig => twig.All(point => point.Width < 0.6f)), Is.True, "schmaler als der Riss");
            Assert.That(twigs.All(twig => twig[^1].Distance > twig[0].Distance), Is.True, "ihr Weg zählt ab dem Anfang des Risses weiter");
        });
    }

    [Test]
    public void Riss_HaeltAbstandZuEinemAnderenRissDaneben()
    {
        //Ein Riss 1,5 m neben der Bahn, ab 3 m vom Start. Ohne Ausweichen käme ein wandernder Riss ihm oft näher als 1,1 m
        var other     = new FissureSegment(300f, 150f, 3000f, 150f);
        var obstacles = new[] { other };

        for (var seed = 0; seed < Seeds; seed++)
        {
            var shape = Trace(seed, 0f, obstacles);

            Assert.Multiple(() =>
            {
                Assert.That(shape.Segments.Any(segment => segment.Intersects(other)), Is.False, $"Seed {seed}");
                Assert.That(shape.Points.Where(point => DistanceFromStart(point) > Style.StartFreeRadius + Style.StepLength)
                                 .Min(point => other.DistanceTo(point.X, point.Y)),
                            Is.GreaterThan(Style.Clearance - RefineSlack),
                            $"Seed {seed}");
            });
        }
    }

    //Wie viel Weg des Risses näher als distance an den Stücken liegt, ohne die ersten skip Pixel
    private static float LengthNear(FissureShape shape, IReadOnlyList<FissureSegment> others, float distance, float skip = 0f)
        => shape.Segments.Zip(shape.Points.Skip(1))
                .Where(pair => pair.Second.Distance > skip && others.Any(other => other.DistanceTo((pair.First.AX + pair.First.BX) / 2f, (pair.First.AY + pair.First.BY) / 2f) < distance))
                .Sum(pair => MathF.Sqrt((pair.First.BX - pair.First.AX) * (pair.First.BX - pair.First.AX) + (pair.First.BY - pair.First.AY) * (pair.First.BY - pair.First.AY)));

    //Der Wunsch des Users vom 08.10.2026: Hauptrisse dürfen sich schneiden, aber nicht parallel verlaufen
    [Test]
    public void Faecher_AusEinemEinschlag_LaeuftNichtNebeneinanderHer()
    {
        for (var seed = 0; seed < Seeds; seed++)
        {
            var random    = new SeededRandom(seed);
            var obstacles = new List<FissureSegment>();
            var shapes    = new List<FissureShape>();

            foreach (var degrees in new[] { -30f, 0f, 30f })
            {
                var shape = FissurePath.Trace(0f, 0f, degrees * MathF.PI / 180f, Length, obstacles, random, null, null, true);

                shapes.Add(shape);
                obstacles.AddRange(shape.Segments);
            }

            //Am Einschlag beginnen alle am selben Punkt, dort zählt es nicht
            for (var a = 0; a < shapes.Count; a++)
            {
                for (var b = 0; b < shapes.Count; b++)
                {
                    if (a == b)
                        continue;

                    var near = LengthNear(shapes[a], shapes[b].Segments.ToList(), 60f, Style.StartFreeRadius + Style.StepLength);

                    Assert.That(near, Is.LessThan(250f), $"Seed {seed}: Riss {a} läuft {near:0} px dicht an Riss {b}");
                }
            }
        }
    }

    [Test]
    public void Hauptriss_KreuztEinenRiss_UndKnicktRechtwinkligAb()
    {
        //Ein Riss quer im Weg, schräg um 45° durch (350, 0): von (-1150, -1500) nach (850, 500). Rechtwinklig von ihm weg zeigt (1, -1).
        //Der Hauptriss wandert, darum erreicht er ihn nicht mit jedem Seed
        var other     = new FissureSegment(-1150f, -1500f, 850f, 500f);
        var obstacles = Enumerable.Range(0, 200).Select(i => new FissureSegment(-1150f + i * 10f, -1500f + i * 10f, -1140f + i * 10f, -1490f + i * 10f)).ToList();
        var across    = new List<float>();
        var crossed   = 0;

        for (var seed = 0; seed < Seeds; seed++)
        {
            var shape    = FissurePath.Trace(0f, 0f, 0f, Length, obstacles, new SeededRandom(seed), null, null, true);
            var crossing = shape.Points.Zip(shape.Points.Skip(1)).Where(pair => new FissureSegment(pair.First.X, pair.First.Y, pair.Second.X, pair.Second.Y).Intersects(other)).ToList();

            Assert.That(shape.Length, Is.EqualTo(Length).Within(0.01f), $"Seed {seed}: er hält nicht an");

            if (crossing.Count == 0)
                continue;

            crossed++;

            //Die Richtung 1,5 m nach dem Kreuzen, gemessen über 1 m
            var at    = crossing[0].First.Distance;
            var after = shape.Points.Where(point => point.Distance > at + 150f && point.Distance < at + 250f).ToList();

            if (after.Count < 2)
                continue;

            var degrees = MathF.Atan2(after[^1].Y - after[0].Y, after[^1].X - after[0].X) * 180f / MathF.PI;

            across.Add(degrees);

            Assert.Multiple(() =>
            {
                Assert.That(crossing, Has.Count.EqualTo(1), $"Seed {seed}: er kreuzt einmal und kehrt nicht zurück");
                Assert.That(degrees, Is.EqualTo(-45f).Within(40f), $"Seed {seed}: nach dem Kreuzen läuft er rechtwinklig weg vom anderen");
            });
        }

        Assert.Multiple(() =>
        {
            Assert.That(crossed, Is.GreaterThanOrEqualTo(Seeds * 4 / 5), "die meisten kreuzen");
            Assert.That(across.Average(), Is.EqualTo(-45f).Within(15f), "im Mittel rechtwinklig");
        });
    }

    [Test]
    public void Hauptriss_LaeuftNichtNebenEinemRissHer()
    {
        //Ein Riss 1,5 m neben der Bahn, parallel. Ohne die Regel liefe der Hauptriss lange in seiner Nähe
        var obstacles = Enumerable.Range(0, 270).Select(i => new FissureSegment(300f + i * 10f, 150f, 310f + i * 10f, 150f)).ToList();

        for (var seed = 0; seed < Seeds; seed++)
        {
            var shape = FissurePath.Trace(0f, 0f, 0f, Length, obstacles, new SeededRandom(seed), null, null, true);

            Assert.That(LengthNear(shape, obstacles, 100f), Is.LessThan(400f), $"Seed {seed}: höchstens ein steiles Kreuzen");
        }
    }

    [Test]
    public void Nebenriss_WeichtAus_HauptrissKreuzt()
    {
        //Ein 6 m langer Riss quer im Weg, um den ein Nebenriss herumkommt
        var wall = Enumerable.Range(0, 60).Select(i => new FissureSegment(500f, -300f + i * 10f, 500f, -290f + i * 10f)).ToList();

        var branch = Trace(1, 0f, wall);
        var main   = FissurePath.Trace(0f, 0f, 0f, Length, wall, new SeededRandom(1), null, null, true);

        Assert.Multiple(() =>
        {
            Assert.That(branch.Segments.Any(segment => wall.Any(segment.Intersects)), Is.False, "der Nebenriss findet Platz vor der Linie");
            Assert.That(main.Segments.Any(segment => wall.Any(segment.Intersects)), Is.True, "der Hauptriss geht hindurch");
        });
    }

    [Test]
    public void OhneFreienWeg_SchneidetDerRissDenAnderen()
    {
        //Ein geschlossenes Quadrat mit 4 m Seite um den Start, der Riss kommt nur hindurch
        var box = new[]
        {
            new FissureSegment(-400f, -400f, 400f, -400f),
            new FissureSegment(400f, -400f, 400f, 400f),
            new FissureSegment(400f, 400f, -400f, 400f),
            new FissureSegment(-400f, 400f, -400f, -400f)
        };

        for (var seed = 0; seed < Seeds; seed++)
        {
            var shape = Trace(seed, 0f, box);

            Assert.Multiple(() =>
            {
                Assert.That(shape.Length, Is.EqualTo(Length).Within(0.01f), $"Seed {seed}: er hält nicht an");
                Assert.That(shape.Segments.Any(segment => box.Any(segment.Intersects)), Is.True, $"Seed {seed}");
            });
        }
    }

    [Test]
    public void Mauer_HaeltDenRissAuf()
    {
        for (var seed = 0; seed < Seeds; seed++)
        {
            var shape = Trace(seed, 0f, null, (_, _, toX, _) => toX > 400f);

            Assert.That(shape.Points.Max(point => point.X), Is.LessThanOrEqualTo(400f + RefineSlack), $"Seed {seed}");
        }

        var walledIn = Trace(1, 0f, null, (_, _, _, _) => true);

        Assert.Multiple(() =>
        {
            Assert.That(walledIn.Points, Has.Count.EqualTo(1), "Mauern ringsum: Der Riss endet am Start");
            Assert.That(walledIn.Length, Is.Zero);
        });
    }

    [Test]
    public void Beruehrung_ZaehltNurDenGewachsenenTeil_MitBreiteUndKoerper()
    {
        var shape = Trace(2);
        var end   = shape.Points[^1];

        Assert.Multiple(() =>
        {
            Assert.That(shape.Touches(0f, 0f, 0f, 25f, 0f), Is.True, "der Anfang ist sofort da");
            Assert.That(shape.Touches(end.X, end.Y, 30f, 25f, 0.5f), Is.False, "die Spitze ist nach der Hälfte der Zeit noch nicht da");
            Assert.That(shape.Touches(end.X, end.Y, 30f, 25f, 1f), Is.True);
            Assert.That(shape.Touches(end.X, end.Y + 50f, 30f, 25f), Is.True, "Körper und halbe Breite reichen bis 55");
            Assert.That(shape.Touches(-200f, 0f, 30f, 25f), Is.False, "hinter dem Start liegt nichts");
            Assert.That(shape.ReachRadius, Is.GreaterThan(Length * 0.25f));
        });
    }

    [Test]
    public void Nebenriss_SuchtDieFreieRichtung()
    {
        //Der Hauptriss läuft vom Gegner nach Osten und Westen, frei ist es nur nach Norden und Süden
        var obstacles = new[] { new FissureSegment(-2000f, 0f, 2000f, 0f) };

        for (var seed = 0; seed < Seeds; seed++)
        {
            var angle = FissurePath.PickFreeAngle(0f, 0f, obstacles, new SeededRandom(seed));

            Assert.That(MathF.Abs(MathF.Sin(angle)), Is.GreaterThan(0.85f), $"Seed {seed}: {angle * 180f / MathF.PI:0}°");
        }
    }

    [Test]
    public void Nebenriss_MeidetMauern()
    {
        for (var seed = 0; seed < Seeds; seed++)
        {
            var angle = FissurePath.PickFreeAngle(0f, 0f, Array.Empty<FissureSegment>(), new SeededRandom(seed), (_, _, toX, _) => toX < 100f);

            Assert.That(MathF.Cos(angle), Is.GreaterThan(0.15f), $"Seed {seed}: frei ist es nur nach Osten");
        }
    }
}
