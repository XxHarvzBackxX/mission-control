using MissionControl.Domain;
using MissionControl.Domain.ValueObjects;

namespace MissionControl.Tests.Domain;

[TestFixture]
public class KerbinTimeTests
{
    [Test]
    public void Decompose_ZeroSeconds_ReturnsAllZeros()
    {
        var time = new KerbinTime(0);
        var (years, days, hours, minutes, seconds) = time.Decompose();

        Assert.Multiple(() =>
        {
            Assert.That(years, Is.EqualTo(0));
            Assert.That(days, Is.EqualTo(0));
            Assert.That(hours, Is.EqualTo(0));
            Assert.That(minutes, Is.EqualTo(0));
            Assert.That(seconds, Is.EqualTo(0));
        });
    }

    [Test]
    public void Decompose_ExactlyOneYear_Returns1y0d0h0m0s()
    {
        var time = new KerbinTime(9_201_600);
        var (years, days, hours, minutes, seconds) = time.Decompose();

        Assert.Multiple(() =>
        {
            Assert.That(years, Is.EqualTo(1));
            Assert.That(days, Is.EqualTo(0));
            Assert.That(hours, Is.EqualTo(0));
            Assert.That(minutes, Is.EqualTo(0));
            Assert.That(seconds, Is.EqualTo(0));
        });
    }

    [Test]
    public void Decompose_MultiYear_DecomposesCorrectly()
    {
        // 2 years + 42 days + 3 hours + 15 minutes + 30 seconds
        long totalSeconds = (2 * 9_201_600) + (42 * 21_600) + (3 * 3_600) + (15 * 60) + 30;
        var time = new KerbinTime(totalSeconds);
        var (years, days, hours, minutes, seconds) = time.Decompose();

        Assert.Multiple(() =>
        {
            Assert.That(years, Is.EqualTo(2));
            Assert.That(days, Is.EqualTo(42));
            Assert.That(hours, Is.EqualTo(3));
            Assert.That(minutes, Is.EqualTo(15));
            Assert.That(seconds, Is.EqualTo(30));
        });
    }

    [Test]
    public void ToDisplayString_FormatsCorrectly()
    {
        long totalSeconds = (1 * 9_201_600) + (42 * 21_600) + (3 * 3_600) + (15 * 60) + 0;
        var time = new KerbinTime(totalSeconds);

        Assert.That(time.ToDisplayString(), Is.EqualTo("1y, 42d, 3h, 15m, 0s"));
    }

    [Test]
    public void Constructor_NegativeSeconds_ThrowsDomainException()
    {
        Assert.That(() => new KerbinTime(-1), Throws.TypeOf<DomainException>());
    }

    [Test]
    public void Decompose_BoundaryOneSecond_Returns0y0d0h0m1s()
    {
        var time = new KerbinTime(1);
        var (years, days, hours, minutes, seconds) = time.Decompose();

        Assert.Multiple(() =>
        {
            Assert.That(years, Is.EqualTo(0));
            Assert.That(days, Is.EqualTo(0));
            Assert.That(hours, Is.EqualTo(0));
            Assert.That(minutes, Is.EqualTo(0));
            Assert.That(seconds, Is.EqualTo(1));
        });
    }

    [Test]
    public void Decompose_ExactlyOneDay_Returns0y1d0h0m0s()
    {
        var time = new KerbinTime(21_600);
        var (years, days, hours, minutes, seconds) = time.Decompose();

        Assert.Multiple(() =>
        {
            Assert.That(years, Is.EqualTo(0));
            Assert.That(days, Is.EqualTo(1));
            Assert.That(hours, Is.EqualTo(0));
            Assert.That(minutes, Is.EqualTo(0));
            Assert.That(seconds, Is.EqualTo(0));
        });
    }
}
