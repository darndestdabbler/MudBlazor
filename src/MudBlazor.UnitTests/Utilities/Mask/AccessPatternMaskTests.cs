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

    // --- Zip code pattern (mixed required/optional digits) ---

    [Test]
    public void ZipCode_RequiredAndOptionalDigits()
    {
        var mask = new AccessPatternMask(@"00000\-9999");
        mask.Insert("123454321");
        mask.Text.Should().Be("12345-4321");
    }

    [Test]
    public void ZipCode_OnlyRequiredFilled()
    {
        var mask = new AccessPatternMask(@"00000\-9999");
        mask.Insert("12345");
        mask.IsMaskComplete.Should().BeTrue();
    }

    // --- Unrecognized characters treated as literal delimiters ---

    [Test]
    public void UnrecognizedChar_TreatedAsLiteralDelimiter()
    {
        // Characters not in the mask token set are literal delimiters.
        var mask = new AccessPatternMask("000-0000");
        mask.Insert("5551234");
        mask.Text.Should().Be("555-1234");
    }

    // --- Null mask input ---

    [Test]
    public void Constructor_NullMask_ThrowsArgumentNullException()
    {
        var act = () => new AccessPatternMask(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // --- Trailing escape ---

    [Test]
    public void TrailingEscape_TreatedAsLiteralDelimiter()
    {
        // A trailing \ has nothing to escape — it becomes a literal backslash delimiter.
        var mask = new AccessPatternMask("00\\");
        mask.Insert("12");
        mask.Text.Should().Be("12\\");
    }

    // --- Northwind phone mask (with optional character extension) ---

    [Test]
    public void NorthwindPhone_WithOptionalExtension()
    {
        // Actual Northwind pattern: \(000\) 000\-0000\ CCCCCCCC
        var mask = new AccessPatternMask(@"\(000\) 000\-0000\ CCCCCCCC");
        mask.Insert("5551234567ext12");
        mask.Text.Should().Be("(555) 123-4567 ext12");
    }

    [Test]
    public void NorthwindPhone_NoExtension_IsMaskComplete()
    {
        var mask = new AccessPatternMask(@"\(000\) 000\-0000\ CCCCCCCC");
        mask.Insert("5551234567");
        // Only 0 positions are required; C positions are optional.
        mask.IsMaskComplete.Should().BeTrue();
    }

    // --- Feature: 'a' (alphanumeric optional) token ---

    [Test]
    public void Insert_AlphanumericOptional_AcceptsLettersAndDigits()
    {
        var mask = new AccessPatternMask("aaa");
        mask.Insert("x1y");
        mask.Text.Should().Be("x1y");
    }

    [Test]
    public void Insert_AlphanumericOptional_AcceptsPartialInput()
    {
        var mask = new AccessPatternMask("aaa");
        mask.Insert("x");
        mask.Text.Should().Be("x");
    }

    [Test]
    public void Insert_AlphanumericOptional_RejectsSpecialChars()
    {
        var mask = new AccessPatternMask("aaa");
        mask.Insert("!@#");
        mask.Text.Should().BeEmpty();
    }

    [Test]
    public void IsMaskComplete_AlphanumericOptional_NotRequired()
    {
        // 'A' is required, 'a' is optional — filling only 'A' should be complete.
        var mask = new AccessPatternMask("Aa");
        mask.Insert("X");
        mask.IsMaskComplete.Should().BeTrue();
    }

    // --- Feature: Semicolon section parsing ---

    [Test]
    public void Semicolon_UsesOnlyFirstSection()
    {
        // "000-0000;_; " — only the mask "000-0000" is used.
        var mask = new AccessPatternMask("000-0000;_;x");
        mask.Insert("5551234");
        mask.Text.Should().Be("555-1234");
    }

    [Test]
    public void Semicolon_NoSemicolon_WorksNormally()
    {
        var mask = new AccessPatternMask("000");
        mask.Insert("123");
        mask.Text.Should().Be("123");
    }

    [Test]
    public void Semicolon_AccessMaskPreservesOriginal()
    {
        var original = "000-0000;_;x";
        var mask = new AccessPatternMask(original);
        mask.AccessMask.Should().Be(original);
    }

    // --- Ported from WPF MaskParser tests ---
    // The following tests were ported from dabbler-wpf MaskParserTests.cs.
    // WPF tests verify parsing (segment types/counts); Blazor equivalents
    // verify the same behavior through insertion and text output.

    [Test]
    public void AllMaskCharacters_CombinedInsertion()
    {
        // WPF: Parse_AllMaskCharacters_ReturnsCorrectTypes
        // Mask "09L?Aa&C" — one of each mask character type.
        // 0=digit req, 9=digit opt, L=letter req, ?=letter opt,
        // A=alnum req, a=alnum opt, &=any req, C=any opt
        var mask = new AccessPatternMask("09L?Aa&C");
        mask.Insert("51Xb3y!@");
        mask.Text.Should().Be("51Xb3y!@");
    }

    [Test]
    public void AllMaskCharacters_RequiredOnly_IsMaskComplete()
    {
        // 0, L, A, & are required; 9, ?, a, C are optional.
        // Fill all 8 positions then verify IsMaskComplete is true.
        var mask = new AccessPatternMask("09L?Aa&C");
        mask.Insert("51Xb3y!@");
        mask.IsMaskComplete.Should().BeTrue();
    }

    [Test]
    public void EmptyMask_ProducesNoOutput()
    {
        // WPF: Parse_EmptyMask_ThrowsArgumentException
        // AccessPatternMask delegates to PatternMask which accepts empty masks.
        var mask = new AccessPatternMask("");
        mask.Insert("123");
        mask.Text.Should().BeEmpty();
    }

    [Test]
    public void WhitespaceMask_ProducesNoEditablePositions()
    {
        // WPF: Parse_WhitespaceMask_ThrowsArgumentException
        // Spaces are not mask tokens — they become literal delimiters.
        // With no editable positions, no input is accepted.
        var mask = new AccessPatternMask("   ");
        mask.Insert("abc");
        mask.Text.Should().BeEmpty();
    }

    [Test]
    public void SSN_AllEditablePositions_AreDigitRequired()
    {
        // WPF: Parse_SSNMask_ParsesCorrectly — verifies all non-literal
        // segments are DigitRequired. Behavioral equivalent: letters rejected,
        // only digits accepted in all 9 positions.
        var mask = new AccessPatternMask("000-00-0000");
        mask.Insert("abcdefghi");
        mask.Text.Should().BeEmpty();

        mask.Insert("123456789");
        mask.Text.Should().Be("123-45-6789");
        mask.IsMaskComplete.Should().BeTrue();
    }

    [Test]
    public void DigitPosition_RejectsSpaces()
    {
        // WPF: MaskSegment_Accepts_DigitRequired — space rejected.
        var mask = new AccessPatternMask("000");
        mask.Insert("1 2");
        // Space is rejected at position 1, so only '1' and '2' are accepted.
        mask.Text.Should().Be("12");
    }

    [Test]
    public void LetterPosition_RejectsSpecialChars()
    {
        // WPF: MaskSegment_Accepts_LetterRequired — non-letter rejected.
        var mask = new AccessPatternMask("LLL");
        mask.Insert("a!b");
        mask.Text.Should().Be("ab");
    }

    [Test]
    public void AlphanumericPosition_RejectsSpecialChars()
    {
        // WPF: MaskSegment_Accepts_AlphanumericRequired — special char rejected.
        var mask = new AccessPatternMask("AAA");
        mask.Insert("a-1");
        mask.Text.Should().Be("a1");
    }

    // --- Feature: Quoted literals ---

    [Test]
    public void QuotedLiteral_TreatsContentAsLiteral()
    {
        // "ab" makes 'a' and 'b' literal, not mask tokens.
        var mask = new AccessPatternMask("00\"ab\"00");
        mask.Insert("1234");
        mask.Text.Should().Be("12ab34");
    }

    [Test]
    public void QuotedLiteral_MaskTokensBecomeLiteral()
    {
        // L inside quotes is literal 'L', not letter-required.
        var mask = new AccessPatternMask("00\"L\"00");
        mask.Insert("1234");
        mask.Text.Should().Be("12L34");
    }

    [Test]
    public void QuotedLiteral_EmptyQuotes_NoEffect()
    {
        var mask = new AccessPatternMask("\"\"000");
        mask.Insert("123");
        mask.Text.Should().Be("123");
    }

    [Test]
    public void QuotedLiteral_UnclosedQuote_TreatsRestAsLiteral()
    {
        // Unclosed quote — everything after the opening quote is literal.
        var mask = new AccessPatternMask("00\"ab");
        mask.Insert("12");
        mask.Text.Should().Be("12ab");
    }

    [Test]
    public void QuotedLiteral_IsMaskComplete_LiteralsNotRequired()
    {
        // Quoted characters are literal delimiters, not required positions.
        var mask = new AccessPatternMask("00\"LL\"00");
        mask.Insert("1234");
        mask.IsMaskComplete.Should().BeTrue();
    }
}
