using System.Text.RegularExpressions;
using UnityEngine;

public static class TextStuff
{
    public static string HP => "HP".Color(Color.darkGreen);
    public static string Green = "#65FF5D";
    public static string Red = "#F84D47";
    public static string Blue => "#0087FF";

    public static string OnTurnEnd => "On turn end:".Color(Color.darkMagenta);
    public static string OnCharge => "On charge:".Color(Color.darkMagenta);
    public static string Damage => "Damage".Color(Color.darkRed);
    public static string Heals => "Heals".Color(Color.darkGreen);

    
    private static readonly Regex ColorTagRegex =
        new Regex(@"<color=(?<c>#[0-9a-fA-F]{6,8}|[a-zA-Z]+)>", RegexOptions.Compiled);

    public static string Block => "Block".Color(Color.darkCyan);
    public static string BreakDebuff => "Break".Color(Color.darkSlateGray);

    public static string ColoredValue(int value, string coloredLabel)
        => ColoredValue(value.ToString(), coloredLabel);

    public static string ColoredValue(float value, string coloredLabel, string format = "0.#")
        => ColoredValue(value.ToString(format), coloredLabel);

    public static string ColoredValue(string valueText, string coloredLabel)
    {
        if (string.IsNullOrEmpty(coloredLabel))
            return valueText;

        var m = ColorTagRegex.Match(coloredLabel);
        if (!m.Success)
            return $"{valueText} {coloredLabel}";

        var c = m.Groups["c"].Value;
        return $"<color={c}>{valueText}</color> {coloredLabel}";
    }

    public static string ColoredRange(int min, int max, string coloredLabel)
    {
        return TextStuff.ColoredValue($"{min} - {max}", coloredLabel);
    }


    public static string ColoredNumber(int value, string coloredLabel)
        => ColoredNumber(value.ToString(), coloredLabel);

    public static string ColoredNumber(float value, string coloredLabel, string format = "0.#")
        => ColoredNumber(value.ToString(format), coloredLabel);

    public static string ColoredNumber(string valueText, string coloredLabel)
    {
        if (string.IsNullOrEmpty(coloredLabel))
            return valueText;

        var m = ColorTagRegex.Match(coloredLabel);
        if (!m.Success)
            return valueText;
        var c = m.Groups["c"].Value;
        return $"<color={c}>{valueText}</color>";
    }
}