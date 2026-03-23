// Copyright (c) MudBlazor 2021
// MudBlazor licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AwesomeAssertions;
using NUnit.Framework;

namespace MudBlazor.UnitTests.Utilities.Mask;

[TestFixture]
public class AccessPatternMaskTests
{
    // --- Basic insertion ---

    [Test]
    public void Insert_DigitRequired_AcceptsDigits()
    {
        var mask = new AccessPatternMask("000-0000");
        mask.Insert("5551234");
        mask.Text.Should().Be("555-1234");
    }

    [Test]
    public void Insert_DigitRequired_RejectsLetters()
    {
        var mask = new AccessPatternMask("000");
        mask.Insert("abc");
        mask.Text.Should().BeEmpty();
    }

    [Test]
    public void Insert_LetterRequired_AcceptsLetters()
    {
        var mask = new AccessPatternMask("LLL");
        mask.Insert("abc");
        mask.Text.Should().Be("abc");
    }

    [Test]
    public void Insert_LetterRequired_RejectsDigits()
    {
        var mask = new AccessPatternMask("LLL");
        mask.Insert("123");
        mask.Text.Should().BeEmpty();
    }

    [Test]
    public void Insert_LetterOptional_AcceptsLetters()
    {
        var mask = new AccessPatternMask("???");
        mask.Insert("xy");
        mask.Text.Should().Be("xy");
    }

    [Test]
    public void Insert_Alphanumeric_AcceptsLettersAndDigits()
    {
        var mask = new AccessPatternMask("AAA");
        mask.Insert("a1b");
        mask.Text.Should().Be("a1b");
    }

    [Test]
    public void Insert_AnyCharRequired_AcceptsAny()
    {
        var mask = new AccessPatternMask("&&&");
        mask.Insert("a1!");
        mask.Text.Should().Be("a1!");
    }

    [Test]
    public void Insert_AnyCharOptional_AcceptsAny()
    {
        var mask = new AccessPatternMask("CCC");
        mask.Insert("x@");
        mask.Text.Should().Be("x@");
    }

    [Test]
    public void Insert_HashSign_AcceptsDigitSpacePlusMinus()
    {
        var mask = new AccessPatternMask("###");
        mask.Insert("1 +");
        mask.Text.Should().Be("1 +");
    }

    [Test]
    public void Insert_HashSign_RejectsLetters()
    {
        var mask = new AccessPatternMask("###");
        mask.Insert("abc");
        mask.Text.Should().BeEmpty();
    }

    // --- Escape sequences ---

    [Test]
    public void Escape_TreatsNextCharAsLiteral()
    {
        // \( and \) are literal parens, not mask tokens
        var mask = new AccessPatternMask(@"\(000\) 000-0000");
        mask.Insert("5551234567");
        mask.Text.Should().Be("(555) 123-4567");
    }

    [Test]
    public void Escape_MaskCharAsLiteral()
    {
        // \L makes 'L' a literal, not "letter required"
        var mask = new AccessPatternMask(@"\L000");
        mask.Insert("123");
        mask.Text.Should().Be("L123");
    }

    // --- Phone number pattern (common Access mask) ---

    [Test]
    public void PhoneNumber_FullInput()
    {
        var mask = new AccessPatternMask(@"\(000\) 000\-0000");
        mask.Insert("2125551234");
        mask.Text.Should().Be("(212) 555-1234");
    }

    // --- SSN pattern ---

    [Test]
    public void SSN_FullInput()
    {
        var mask = new AccessPatternMask("000-00-0000");
        mask.Insert("123456789");
        mask.Text.Should().Be("123-45-6789");
    }

    // --- Mixed required and optional ---

    [Test]
    public void MixedMask_RequiredAndOptional()
    {
        var mask = new AccessPatternMask("L9L 9L9");
        mask.Insert("K2M5B4");
        mask.Text.Should().Be("K2M 5B4");
    }

    // --- IsMaskComplete ---

    [Test]
    public void IsMaskComplete_AllRequiredFilled_ReturnsTrue()
    {
        var mask = new AccessPatternMask("000-0000");
        mask.Insert("5551234");
        mask.IsMaskComplete.Should().BeTrue();
    }

    [Test]
    public void IsMaskComplete_PartialInput_ReturnsFalse()
    {
        var mask = new AccessPatternMask("000-0000");
        mask.Insert("555");
        mask.IsMaskComplete.Should().BeFalse();
    }

    [Test]
    public void IsMaskComplete_EmptyInput_ReturnsFalse()
    {
        var mask = new AccessPatternMask("000-0000");
        mask.IsMaskComplete.Should().BeFalse();
    }

    [Test]
    public void IsMaskComplete_WithOptionalPositions_OnlyChecksRequired()
    {
        // L=required, 9=optional: "A" fills the required L, optional 9 can be empty
        var mask = new AccessPatternMask("L9");
        mask.Insert("A");
        mask.IsMaskComplete.Should().BeTrue();
    }

    [Test]
    public void IsMaskComplete_AllOptional_EmptyInput_ReturnsFalse()
    {
        var mask = new AccessPatternMask("999");
        mask.IsMaskComplete.Should().BeFalse();
    }

    [Test]
    public void IsMaskComplete_WithPlaceholder_UnfilledRequired_ReturnsFalse()
    {
        var mask = new AccessPatternMask("000-0000") { Placeholder = '_' };
        mask.Insert("555");
        mask.IsMaskComplete.Should().BeFalse();
    }

    [Test]
    public void IsMaskComplete_WithPlaceholder_AllFilled_ReturnsTrue()
    {
        var mask = new AccessPatternMask("000-0000") { Placeholder = '_' };
        mask.Insert("5551234");
        mask.IsMaskComplete.Should().BeTrue();
    }

    // --- GetCleanText ---

    [Test]
    public void GetCleanText_WithCleanDelimiters_RemovesDelimiters()
    {
        var mask = new AccessPatternMask("000-0000") { CleanDelimiters = true };
        mask.Insert("5551234");
        mask.GetCleanText().Should().Be("5551234");
    }

    [Test]
    public void GetCleanText_WithoutCleanDelimiters_KeepsDelimiters()
    {
        var mask = new AccessPatternMask("000-0000") { CleanDelimiters = false };
        mask.Insert("5551234");
        mask.GetCleanText().Should().Be("555-1234");
    }

    // --- Backspace and Delete ---

    [Test]
    public void Backspace_RemovesCharacter()
    {
        var mask = new AccessPatternMask("000");
        mask.Insert("123");
        mask.Backspace();
        mask.Text.Should().Be("12");
    }

    [Test]
    public void Delete_RemovesCharacterAtCaret()
    {
        var mask = new AccessPatternMask("000");
        mask.Insert("123");
        mask.CaretPos = 1;
        mask.Delete();
        mask.Text.Should().Be("13");
    }

    // --- Clear ---

    [Test]
    public void Clear_ResetsTextAndCaret()
    {
        var mask = new AccessPatternMask("000");
        mask.Insert("123");
        mask.Clear();
        mask.Text.Should().BeEmpty();
        mask.CaretPos.Should().Be(0);
    }

    // --- SetText ---

    [Test]
    public void SetText_ValidInput_SetsText()
    {
        var mask = new AccessPatternMask("000-0000");
        mask.SetText("5551234");
        mask.Text.Should().Be("555-1234");
    }

    // --- AccessMask property ---

    [Test]
    public void AccessMask_PreservesOriginalMask()
    {
        var original = @"\(000\) 000-0000";
        var mask = new AccessPatternMask(original);
        mask.AccessMask.Should().Be(original);
    }

    // --- Digit-optional positions accept digits ---

    [Test]
    public void DigitOptional_AcceptsDigits()
    {
        var mask = new AccessPatternMask("99-99");
        mask.Insert("1234");
        mask.Text.Should().Be("12-34");
    }

    // --- Canadian postal code pattern ---

    [Test]
    public void CanadianPostalCode_FullInput()
    {
        // Canadian postal code: letter digit letter space digit letter digit
        var mask = new AccessPatternMask("L0L 0L0");
        mask.Insert("K2M5B4");
        mask.Text.Should().Be("K2M 5B4");
    }
}
