namespace Broker.Backoffice.Application.Common;

public static class LikeHelper
{
    public static string EscapeLike(string input)
        => input.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");

    public static string ContainsPattern(string input)
        => $"%{EscapeLike(input)}%";
}
