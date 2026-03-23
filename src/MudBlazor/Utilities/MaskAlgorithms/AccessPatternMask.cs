// Copyright (c) MudBlazor 2021
// MudBlazor licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace MudBlazor;

/// <summary>
/// An input mask supporting Microsoft Access mask syntax.
/// </summary>
/// <remarks>
/// <para>
/// The following mask characters are supported:
/// </para>
/// <list type="table">
///   <listheader><term>Character</term><description>Meaning</description></listheader>
///   <item><term><c>0</c></term><description>Digit (0–9), required.</description></item>
///   <item><term><c>9</c></term><description>Digit (0–9), optional.</description></item>
///   <item><term><c>#</c></term><description>Digit, space, or plus/minus sign, optional.</description></item>
///   <item><term><c>L</c></term><description>Letter (a–z, A–Z), required.</description></item>
///   <item><term><c>?</c></term><description>Letter (a–z, A–Z), optional.</description></item>
///   <item><term><c>A</c></term><description>Letter or digit, required.</description></item>
///   <item><term><c>&amp;</c></term><description>Any character, required.</description></item>
///   <item><term><c>C</c></term><description>Any character, optional.</description></item>
///   <item><term><c>\</c></term><description>Escape: treats the next character as a literal delimiter.</description></item>
/// </list>
/// <para>
/// Characters not in the table above are treated as literal delimiters and are displayed as-is.
/// </para>
/// </remarks>
/// <seealso cref="PatternMask" />
/// <seealso cref="BlockMask" />
public class AccessPatternMask : PatternMask
{
    // Mask characters used by Access, mapped to MudBlazor MaskChar definitions.
    // Required vs optional is tracked separately in _requiredPositions.
    private static readonly MaskChar[] AccessMaskChars =
    [
        MaskChar.Digit('0'),                              // Digit required
        MaskChar.Digit('9'),                              // Digit optional
        new MaskChar('#', @"[\d +\-]"),                   // Digit, space, or +/- optional
        MaskChar.Letter('L'),                             // Letter required
        MaskChar.Letter('?'),                             // Letter optional
        MaskChar.LetterOrDigit('A'),                      // Alphanumeric required
        new MaskChar('&', @"."),                           // Any character required
        new MaskChar('C', @"."),                           // Any character optional
    ];

    // Characters that represent required (non-optional) positions in Access mask syntax.
    private static readonly HashSet<char> RequiredChars = ['0', 'L', 'A', '&'];

    // Characters that are valid Access mask tokens (both required and optional).
    private static readonly HashSet<char> AllMaskTokens = ['0', '9', '#', 'L', '?', 'A', '&', 'C'];

    // Unicode Private Use Area base. Escaped characters that conflict with mask tokens
    // are replaced with PUA characters (U+E000+) in the internal mask, then swapped back
    // in the final display text.
    private const char PuaBase = '\uE000';

    private readonly bool[] _requiredPositions;
    private readonly Dictionary<char, char> _puaToOriginal;

    /// <summary>
    /// The original Access-format mask string before escape preprocessing.
    /// </summary>
    public string AccessMask { get; }

    /// <summary>
    /// Creates a new mask using Microsoft Access mask syntax.
    /// </summary>
    /// <param name="accessMask">The Access-format mask string (e.g., <c>\(000\) 000-0000</c>).</param>
    public AccessPatternMask(string accessMask)
        : base(PreprocessMask(accessMask, out var requiredPositions, out var puaToOriginal))
    {
        AccessMask = accessMask;
        _requiredPositions = requiredPositions;
        _puaToOriginal = puaToOriginal;
        MaskChars = AccessMaskChars;
    }

    /// <summary>
    /// Whether all required positions in the mask have been filled with valid input.
    /// </summary>
    /// <remarks>
    /// Returns <c>true</c> when every position marked as required (mask characters
    /// <c>0</c>, <c>L</c>, <c>A</c>, <c>&amp;</c>) contains a non-placeholder character.
    /// Returns <c>false</c> if the text is empty or any required position is unfilled.
    /// </remarks>
    public bool IsMaskComplete
    {
        get
        {
            var text = Text;
            if (string.IsNullOrEmpty(text))
                return false;

            for (var i = 0; i < _requiredPositions.Length; i++)
            {
                if (!_requiredPositions[i])
                    continue;

                if (i >= text.Length)
                    return false;

                // If the character at this position is the placeholder, it's not filled.
                if (Placeholder.HasValue && text[i] == Placeholder.Value)
                    return false;
            }

            return true;
        }
    }

    /// <inheritdoc />
    protected override string ModifyFinalText(string text)
    {
        if (_puaToOriginal.Count == 0)
            return text;

        // Replace any PUA stand-in characters back to their original literals.
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            sb.Append(_puaToOriginal.TryGetValue(c, out var original) ? original : c);
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public override void UpdateFrom(IMask? mask)
    {
        base.UpdateFrom(mask);
        if (mask is AccessPatternMask)
        {
            ForceReinitialize();
            Refresh();
        }
    }

    /// <summary>
    /// Preprocesses an Access-format mask string into a <see cref="PatternMask"/>-compatible mask.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Handles the <c>\</c> escape character. When an escaped character conflicts with a
    /// mask token (e.g., <c>\L</c> where <c>L</c> is normally "letter required"), it is
    /// replaced with a Unicode Private Use Area character in the internal mask. The
    /// <see cref="ModifyFinalText"/> override swaps these back to the original literal
    /// in the displayed text.
    /// </para>
    /// </remarks>
    private static string PreprocessMask(
        string accessMask,
        out bool[] requiredPositions,
        out Dictionary<char, char> puaToOriginal)
    {
        ArgumentNullException.ThrowIfNull(accessMask);

        var processed = new StringBuilder(accessMask.Length);
        var required = new List<bool>();
        puaToOriginal = new Dictionary<char, char>();
        var nextPua = PuaBase;
        var i = 0;

        while (i < accessMask.Length)
        {
            var c = accessMask[i];

            if (c == '\\' && i + 1 < accessMask.Length)
            {
                var escaped = accessMask[i + 1];

                if (AllMaskTokens.Contains(escaped))
                {
                    // Escaped mask token — use PUA stand-in so BaseMask treats it as delimiter.
                    var pua = nextPua++;
                    puaToOriginal[pua] = escaped;
                    processed.Append(pua);
                }
                else
                {
                    // Escaped non-token — pass through directly as delimiter.
                    processed.Append(escaped);
                }

                required.Add(false);
                i += 2;
            }
            else if (AllMaskTokens.Contains(c))
            {
                // Access mask token — pass through.
                processed.Append(c);
                required.Add(RequiredChars.Contains(c));
                i++;
            }
            else
            {
                // Literal delimiter — pass through as-is.
                processed.Append(c);
                required.Add(false);
                i++;
            }
        }

        requiredPositions = required.ToArray();
        return processed.ToString();
    }
}
