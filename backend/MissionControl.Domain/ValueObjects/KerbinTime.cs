namespace MissionControl.Domain.ValueObjects;

public readonly record struct KerbinTime
{
    private const long SecondsPerMinute = 60;
    private const long SecondsPerHour = 3_600;
    private const long SecondsPerDay = 21_600;
    private const long SecondsPerYear = 9_201_600;

    public long TotalSeconds { get; }

    public KerbinTime(long totalSeconds)
    {
        if (totalSeconds < 0)
            throw new DomainException("Kerbin time cannot be negative.");

        TotalSeconds = totalSeconds;
    }

    public (long Years, long Days, long Hours, long Minutes, long Seconds) Decompose()
    {
        var remaining = TotalSeconds;
        var years = remaining / SecondsPerYear;
        remaining %= SecondsPerYear;
        var days = remaining / SecondsPerDay;
        remaining %= SecondsPerDay;
        var hours = remaining / SecondsPerHour;
        remaining %= SecondsPerHour;
        var minutes = remaining / SecondsPerMinute;
        var seconds = remaining % SecondsPerMinute;

        return (years, days, hours, minutes, seconds);
    }

    /// <summary>
    /// Formats the Kerbin time as "Yy, Dd, Hh, Mm, Ss" display string.
    /// </summary>
    public string ToDisplayString()
    {
        var (years, days, hours, minutes, seconds) = Decompose();
        return $"{years}y, {days}d, {hours}h, {minutes}m, {seconds}s";
    }
}
