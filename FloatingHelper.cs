using System.Numerics;

namespace Maxscript_FloatingPrecision;

/// <summary>
/// Provides floating operations, which tries 
/// to emulate the behaviour of Maxscript.
/// </summary>
public static class FloatingHelper
{
    /// <summary>
    /// Calculates less precision value, by using <paramref name="precisionShift"/>
    /// as a virtual mask for a resulting precision index.
    /// <para>The calculation for precision index is done by subtracting the count
    /// of digits in a <paramref name="value"/> and <paramref name="precisionShift"/>.</para>
    /// </summary>
    public static T GetPrecisionValue<T>(T value, int precisionShift)
        where T : INumber<T>, IFloatingPoint<T>
    {
        T precisionMask = T.CreateChecked(Math.Pow(10, precisionShift * -1));
        if(T.CreateChecked(Math.Abs(decimal.CreateChecked(value))) < precisionMask) return T.CreateChecked(0.0);

        int currentShiftCount = Math.Clamp(GetDecimalPointCount(decimal.CreateChecked(value)), 0, 16); if(currentShiftCount == 0) return value;
        long shiftIndex = (long)Math.Pow(10, currentShiftCount);

        long maskValue = long.CreateChecked(value * T.CreateChecked(shiftIndex));
        int relativePrecisionIndex = (int)Math.Floor(Math.Log10(Math.Abs(maskValue)) + 1) - precisionShift;

        long precisionMaskValue = (long)((decimal)maskValue / (long)Math.Pow(10, relativePrecisionIndex));
        return T.CreateChecked(precisionMaskValue) / T.CreateChecked((long)Math.Pow(10, currentShiftCount - relativePrecisionIndex));
    }

    /// <summary>
    /// Calculates round value, which is being determinated by
    /// the count of digits in <paramref name="value"/> and
    /// related <paramref name="shift"/> value.
    /// </summary>
    public static T GetShiftRound<T>(T value, int shift)
        where T : INumber<T>, IFloatingPoint<T>
    {
        int originShiftCount = GetDecimalPointCount(decimal.CreateChecked(value));
        int valueIndexer = Math.Clamp(Math.Abs(int.CreateChecked(value)), 0, 1); shift -= valueIndexer;

        int originRelativeShiftCount = GetDecimalBitwisePointCount(value, originShiftCount);
        if(originRelativeShiftCount + valueIndexer <= shift) return value;

        decimal currentRoundValue = decimal.CreateChecked(RoundOnPrecision(decimal.CreateChecked(value)));
        int currentShiftCount = GetDecimalPointCount(currentRoundValue);

        int currentRelativeShiftCount = GetDecimalBitwisePointCount(currentRoundValue, currentShiftCount) - valueIndexer;
        return currentRelativeShiftCount > shift - valueIndexer ? T.CreateChecked(RoundOnPrecision(currentRoundValue, 1)) : T.CreateChecked(currentRoundValue);
    }

    //The limitation of this calculation is 128 bits, due to hard casting
    //to decimal type, which only supports up to 28 digits.
    private static T RoundOnPrecision<T>(T value, int precisionShift = 1)
        where T : INumber<T>, IFloatingPoint<T>
    {
        int currentPrecision = GetDecimalPointCount(value);
        int precisionDifference = Math.Clamp(currentPrecision - precisionShift, 0, currentPrecision);
        long shiftIndex = (long)Math.Pow(10, precisionDifference);

        decimal roundDecimalValue = Math.Round(decimal.CreateChecked(value) * shiftIndex, MidpointRounding.AwayFromZero);
        return T.CreateChecked(roundDecimalValue / shiftIndex);
    }

    private static int GetDecimalPointCount<T>(T value)
        where T : INumber<T>, IFloatingPoint<T>
    {
        T decimalValue = GetDecimalValue(value);
        T absoluteDecimalIndexer = T.CreateChecked(1) - decimalValue;
        return int.CreateChecked((decimal.GetBits(decimal.CreateChecked(absoluteDecimalIndexer))[3] >> 16) & 255);
    }
 
    private static int GetDecimalBitwisePointCount<T>(T value, int currentDecimalCount)
        where T : INumber<T>, IFloatingPoint<T>
    {
        T decimalValue = GetDecimalValue(value);
        if(decimalValue == T.CreateChecked(0)) return 0;

        long relativeShiftIndex = (long)Math.Pow(10, currentDecimalCount);
        long relativeCastValue = long.CreateChecked(decimalValue * T.CreateChecked(relativeShiftIndex));

        int currentShiftValue = (int)Math.Floor(Math.Log10(Math.Abs((int)relativeCastValue)) + 1);
        int shiftIndex = (int)Math.Pow(10, currentShiftValue);

        T currentDecimalIndexer = T.CreateChecked(decimal.CreateChecked((int)relativeCastValue) / shiftIndex);
        return int.CreateChecked((decimal.GetBits(decimal.CreateChecked(currentDecimalIndexer))[3] >> 16) & 255);
    }

    private static T GetDecimalValue<T>(T value)
        where T : INumber<T>, IFloatingPoint<T>
    {
        T originValue = T.CreateChecked(int.CreateChecked(value));
        return value - originValue;
    }
}
