using Cine.Application.Interfaces;
using Cine.Application.Services;
using FluentAssertions;

namespace cine_back.Tests.Services;

public class InMemoryOtpCodeStoreTests
{
    private sealed class StubTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public StubTimeProvider(DateTimeOffset initialUtc)
        {
            _utcNow = initialUtc;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan delta)
        {
            _utcNow = _utcNow.Add(delta);
        }
    }

    [Fact]
    public void SaveCodeAndValidateCode_WhenValidAndUsedOnce_SucceedsThenExpiresReuse()
    {
        var time = new StubTimeProvider(new DateTimeOffset(2026, 11, 11, 10, 0, 0, TimeSpan.Zero));
        var store = new InMemoryOtpCodeStore(time);

        store.SaveCode("7731234567", "123456", TimeSpan.FromMinutes(10));

        var firstCheck = store.ValidateCode("7731234567", "123456");
        firstCheck.Status.Should().Be(OtpValidationStatus.Success);

        var secondCheck = store.ValidateCode("7731234567", "123456");
        secondCheck.Status.Should().Be(OtpValidationStatus.NotFoundOrExpired);
    }

    [Fact]
    public void TryGetCooldownSeconds_Respects60SecondWindowAndExpires()
    {
        var time = new StubTimeProvider(new DateTimeOffset(2026, 11, 11, 10, 0, 0, TimeSpan.Zero));
        var store = new InMemoryOtpCodeStore(time);

        store.TryGetCooldownSeconds("5512345678", out _).Should().BeFalse();

        store.SaveCode("5512345678", "654321", TimeSpan.FromMinutes(10));
        store.TryGetCooldownSeconds("5512345678", out var remaining).Should().BeTrue();
        remaining.Should().Be(60);

        time.Advance(TimeSpan.FromSeconds(45));
        store.TryGetCooldownSeconds("5512345678", out remaining).Should().BeTrue();
        remaining.Should().Be(15);

        time.Advance(TimeSpan.FromSeconds(16));
        store.TryGetCooldownSeconds("5512345678", out _).Should().BeFalse();
    }

    [Fact]
    public void ValidateCode_WhenExpired_ReturnsNotFoundOrExpired()
    {
        var time = new StubTimeProvider(new DateTimeOffset(2026, 11, 11, 10, 0, 0, TimeSpan.Zero));
        var store = new InMemoryOtpCodeStore(time);

        store.SaveCode("5512345678", "111222", TimeSpan.FromMinutes(10));
        time.Advance(TimeSpan.FromMinutes(11));

        store.TryGetCooldownSeconds("5512345678", out _).Should().BeFalse();
        var result = store.ValidateCode("5512345678", "111222");
        result.Status.Should().Be(OtpValidationStatus.NotFoundOrExpired);
    }

    [Fact]
    public void ValidateCode_WhenExceedingMaxAttempts_LocksOutAndRemovesCode()
    {
        var time = new StubTimeProvider(new DateTimeOffset(2026, 11, 11, 10, 0, 0, TimeSpan.Zero));
        var store = new InMemoryOtpCodeStore(time);

        store.SaveCode("7731234567", "987654", TimeSpan.FromMinutes(10), maxAttempts: 3);

        var r1 = store.ValidateCode("7731234567", "000001");
        r1.Status.Should().Be(OtpValidationStatus.InvalidCode);
        r1.RemainingAttempts.Should().Be(2);

        var r2 = store.ValidateCode("7731234567", "000002");
        r2.Status.Should().Be(OtpValidationStatus.InvalidCode);
        r2.RemainingAttempts.Should().Be(1);

        var r3 = store.ValidateCode("7731234567", "000003");
        r3.Status.Should().Be(OtpValidationStatus.TooManyAttempts);
        r3.RemainingAttempts.Should().Be(0);

        var r4 = store.ValidateCode("7731234567", "987654");
        r4.Status.Should().Be(OtpValidationStatus.NotFoundOrExpired);
    }

    [Fact]
    public void RemoveCode_RemovesExistingEntryAndCleanupRemovesExpiredEntries()
    {
        var time = new StubTimeProvider(new DateTimeOffset(2026, 11, 11, 10, 0, 0, TimeSpan.Zero));
        var store = new InMemoryOtpCodeStore(time);

        store.SaveCode("7731112233", "111111", TimeSpan.FromMinutes(5));
        store.RemoveCode("7731112233");
        store.ValidateCode("7731112233", "111111").Status.Should().Be(OtpValidationStatus.NotFoundOrExpired);

        store.SaveCode("7739998877", "222222", TimeSpan.FromMinutes(5));
        time.Advance(TimeSpan.FromMinutes(6));
        store.SaveCode("7738887766", "333333", TimeSpan.FromMinutes(5));

        store.ValidateCode("7739998877", "222222").Status.Should().Be(OtpValidationStatus.NotFoundOrExpired);
        store.ValidateCode("7738887766", "333333").Status.Should().Be(OtpValidationStatus.Success);
    }
}
