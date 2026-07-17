using Backend.DTOs;
using Backend.Services.Profiles;
using Shouldly;

namespace Backend.Tests.Unit;

public class ProfileHandleValidatorTests
{
    [Fact]
    public void Normalize_TrimsAndLowercasesHandle()
    {
        ProfileHandleValidator.Normalize(" Example-Handle ").ShouldBe("example-handle");
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("valid-handle")]
    [InlineData("valid_handle1")]
    public void GetFormatError_ValidHandle_ReturnsNull(string handle)
    {
        ProfileHandleValidator.GetFormatError(handle).ShouldBeNull();
    }

    [Theory]
    [InlineData("abc", HandleUnavailableReason.TooShort)]
    [InlineData("handle-that-is-too-long", HandleUnavailableReason.TooLong)]
    [InlineData("1handle", HandleUnavailableReason.InvalidFormat)]
    [InlineData("Handle", HandleUnavailableReason.InvalidFormat)]
    [InlineData("handle_", HandleUnavailableReason.InvalidFormat)]
    [InlineData("handle--name", HandleUnavailableReason.InvalidFormat)]
    public void GetFormatError_InvalidHandle_ReturnsReason(
        string handle,
        HandleUnavailableReason expected
    )
    {
        ProfileHandleValidator.GetFormatError(handle).ShouldBe(expected);
    }
}
