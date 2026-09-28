using Hoellenspiralenspiel.Scripts.Core.Enemies;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Enemies;

[TestFixture]
public class EnemyScalingTests
{
    [TestCase(1, 0, 0, 1)]
    [TestCase(5, 2, 0, 7)]
    [TestCase(5, 2, 3, 10)]
    [TestCase(5, -2, 0, 3)]
    [TestCase(1, -5, 0, 1)]
    [TestCase(99, 5, 5, 100)]
    [TestCase(0, 0, 0, 1)]
    public void Level_IstBereichslevelPlusAnpassungen(int areaLevel, int enemyOffset, int spawnOffset, int expected)
        => Assert.That(EnemyScaling.GetLevel(areaLevel, enemyOffset, spawnOffset), Is.EqualTo(expected));

    [TestCase(3, 0.5f, 1, 3)]
    [TestCase(3, 0.5f, 2, 3)]
    [TestCase(3, 0.5f, 3, 4)]
    [TestCase(3, 0.5f, 11, 8)]
    [TestCase(3, 2f, 10, 21)]
    [TestCase(3, 0f, 50, 3)]
    [TestCase(3, -1f, 50, 3)]
    [TestCase(0, 0f, 1, 1)]
    public void Attribut_WaechstMitDemLevel(int valueAtLevelOne, float growthPerLevel, int level, int expected)
        => Assert.That(EnemyScaling.GetAttribute(valueAtLevelOne, growthPerLevel, level), Is.EqualTo(expected));

    [TestCase(100, 1f, 100)]
    [TestCase(100, 1.5f, 150)]
    [TestCase(15, 2.5f, 38)]
    [TestCase(100, 0f, 0)]
    [TestCase(100, -1f, 0)]
    public void Xp_FolgtDemFaktorDerSeltenheit(int baseXp, float factor, int expected)
        => Assert.That(EnemyScaling.GetXp(baseXp, factor), Is.EqualTo(expected));
}
