namespace DotNetCoreProjectConvertor.Extensions
{
    public static class StringExtensions
    {
        public static string StripExtension(this string input)
        {
            return input.Substring(0, input.LastIndexOf("."));
        }
    }
}
