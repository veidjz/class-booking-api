using ClassBooking.Application.Abstractions.Auth;
using ClassBooking.Infrastructure.Auth;
using FluentAssertions;

namespace ClassBooking.IntegrationTests.Auth;

public sealed class BcryptPasswordHasherTests
{
  private const string Password = "s3nh4-segura";

  private readonly IPasswordHasher _hasher = new BcryptPasswordHasher();

  [Fact]
  public void should_verify_a_password_against_its_own_hash()
  {
    string hash = _hasher.Hash(Password);

    _hasher.Verify(Password, hash).Should().BeTrue();
  }

  [Fact]
  public void should_reject_a_password_that_does_not_match_the_hash()
  {
    string hash = _hasher.Hash(Password);

    _hasher.Verify("outra-senha", hash).Should().BeFalse();
  }

  [Fact]
  public void should_hash_with_work_factor_twelve()
  {
    string hash = _hasher.Hash(Password);

    hash.Should().StartWith("$2a$12$");
  }

  [Fact]
  public void should_produce_a_distinct_hash_for_each_call()
  {
    string first = _hasher.Hash(Password);
    string second = _hasher.Hash(Password);

    first.Should().NotBe(second);
  }

  [Theory]
  [InlineData("")]
  [InlineData("hash")]
  [InlineData("$2a$12$")]
  [InlineData("$9z$99$notarealhashvalue")]
  public void should_reject_a_password_when_the_stored_hash_cannot_be_read(string storedHash)
  {
    _hasher.Verify(Password, storedHash).Should().BeFalse();
  }

  [Fact]
  public void should_not_ask_for_a_rehash_of_a_current_hash()
  {
    string hash = _hasher.Hash(Password);

    _hasher.NeedsRehash(hash).Should().BeFalse();
  }

  [Fact]
  public void should_ask_for_a_rehash_of_a_hash_below_the_current_work_factor()
  {
    const string weakerHash = "$2a$11$t1jHY4W/8bgg8Ntn.4u3ru8gIPwPL07cevzP3rZ3KJz1HLk1mBaZC";

    _hasher.NeedsRehash(weakerHash).Should().BeTrue();
  }

  [Theory]
  [InlineData("")]
  [InlineData("hash")]
  [InlineData("$2a$12$")]
  [InlineData("$9z$99$notarealhashvalue")]
  public void should_not_ask_for_a_rehash_when_the_stored_hash_cannot_be_read(string storedHash)
  {
    _hasher.NeedsRehash(storedHash).Should().BeFalse();
  }

  [Fact]
  public void should_distinguish_passwords_that_differ_after_the_seventy_two_byte_truncation_point()
  {
    string prefix = new string('a', 72);
    string original = prefix + new string('b', 28);
    string variation = prefix + new string('c', 28);

    string hash = _hasher.Hash(original);

    _hasher.Verify(variation, hash).Should().BeFalse();
  }
}
