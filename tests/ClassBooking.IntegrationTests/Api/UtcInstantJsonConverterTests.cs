using System.Text.Json;
using ClassBooking.Api.Serialization;
using FluentAssertions;

namespace ClassBooking.IntegrationTests.Api;

public sealed class UtcInstantJsonConverterTests
{
  private static readonly JsonSerializerOptions Options =
      new JsonSerializerOptions { Converters = { new UtcInstantJsonConverter() } };

  [Theory]
  [InlineData(0, "2026-03-02T12:00:00Z")]
  [InlineData(5_000_000, "2026-03-02T12:00:00Z")]
  [InlineData(1_234_560, "2026-03-02T12:00:00Z")]
  public void should_write_the_instant_with_second_precision(long extraTicks, string expected)
  {
    DateTimeOffset instant =
        new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero).AddTicks(extraTicks);

    JsonSerializer.Serialize(instant, Options).Should().Be($"\"{expected}\"");
  }

  [Fact]
  public void should_write_an_offset_instant_as_utc()
  {
    DateTimeOffset instant = new DateTimeOffset(2026, 3, 2, 9, 0, 0, TimeSpan.FromHours(-3));

    JsonSerializer.Serialize(instant, Options).Should().Be("\"2026-03-02T12:00:00Z\"");
  }

  [Fact]
  public void should_read_an_offset_instant_as_utc()
  {
    DateTimeOffset instant = JsonSerializer.Deserialize<DateTimeOffset>("\"2026-03-02T09:00:00-03:00\"", Options);

    instant.Offset.Should().Be(TimeSpan.Zero);
    instant.Should().Be(new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero));
  }

  [Fact]
  public void should_read_an_instant_without_offset_as_utc()
  {
    DateTimeOffset instant = JsonSerializer.Deserialize<DateTimeOffset>("\"2026-03-02T12:00:00\"", Options);

    instant.Should().Be(new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero));
  }

  [Theory]
  [InlineData("\"2026-03-02T25:00:00Z\"")]
  [InlineData("\"tomorrow\"")]
  [InlineData("\"\"")]
  [InlineData("42")]
  [InlineData("null")]
  public void should_report_an_unreadable_instant_as_a_json_failure(string json)
  {
    Action read = () => JsonSerializer.Deserialize<DateTimeOffset>(json, Options);

    read.Should().Throw<JsonException>();
  }
}
