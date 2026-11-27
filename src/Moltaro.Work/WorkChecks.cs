namespace Moltaro.Work;

// The rules that several public members share, so that each rule lives once. The schemas of the providers rely on the
// rules of names, keys, scopes and payloads: their column lengths, their binary collations and their text without
// U+0000. The queue and the worker share the checks of options. Each check reports the ParamName it is given.
internal static class WorkChecks
{
    private const int MaxQueueNameLength = 100;

    private const int MaxKeyLength = 200;

    // The longest TimeSpan of a queue's options.
    internal static readonly TimeSpan LongestQueueTime = TimeSpan.FromDays(3650);

    // The longest timer of TimeProvider: the most a TimeSpan of a worker's options may be.
    internal static readonly TimeSpan LongestTimer = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    internal static void CheckQueueName(string name, string paramName)
    {
        ArgumentNullException.ThrowIfNull(name, paramName);

        if (name.Length is 0 or > MaxQueueNameLength || !name.All(IsQueueNameCharacter))
        {
            throw new ArgumentException(
                $"A queue name is 1 to {MaxQueueNameLength} characters, each one of A-Z a-z 0-9 _ -.",
                paramName);
        }
    }

    // A key and a scope share one rule. The trailing space is refused because SQL Server ignores it when it compares.
    internal static void CheckKeyOrScope(string value, string paramName)
    {
        bool fits = value.Length is > 0 and <= MaxKeyLength
            && value[^1] != ' '
            && !value.Any(char.IsControl)
            && !HasUnpairedSurrogate(value);

        if (!fits)
        {
            throw new ArgumentException(
                $"A key or a scope is 1 to {MaxKeyLength} characters, with no control character and no unpaired surrogate, and does not end with a space.",
                paramName);
        }
    }

    // U+0000 is refused because PostgreSQL cannot store it in text, an unpaired surrogate because UTF-8 cannot encode it.
    internal static void CheckPayload(string payload, string paramName)
    {
        if (payload.Contains('\0') || HasUnpairedSurrogate(payload))
        {
            throw new ArgumentException("A payload contains no U+0000 and no unpaired surrogate.", paramName);
        }
    }

    internal static void CheckTimeSpan(TimeSpan value, TimeSpan longest, string paramName)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero, paramName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, longest, paramName);
    }

    internal static void CheckRange(int value, int lowest, int highest, string paramName)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, lowest, paramName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, highest, paramName);
    }

    private static bool IsQueueNameCharacter(char character)
    {
        return char.IsAsciiLetterOrDigit(character) || character is '_' or '-';
    }

    private static bool HasUnpairedSurrogate(string value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];

            if (char.IsHighSurrogate(character) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
            {
                // A pair: its low half is skipped.
                index++;
            }
            else if (char.IsSurrogate(character))
            {
                return true;
            }
        }

        return false;
    }
}
