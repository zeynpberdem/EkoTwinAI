namespace EkoTwin.Tests.Helpers;

/// <summary>Testlerde "bugün"ü sabitlemek için (örn. gelecek yıl kontrolü).</summary>
public sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset now) => _now = now;

    public static FixedTimeProvider At(int year, int month = 6, int day = 15) =>
        new(new DateTimeOffset(year, month, day, 12, 0, 0, TimeSpan.Zero));

    public override DateTimeOffset GetUtcNow() => _now;
}
