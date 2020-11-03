using System.Text.RegularExpressions;

namespace PDR.PatientBooking.Service.Extensions
{
    public static class StringExtensions
    {
        private static Regex _emailRegex =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        public static bool IsEmailAddress(this string sourceString)
        {
            if (string.IsNullOrEmpty(sourceString))
            {
                return false;
            }

            return _emailRegex.IsMatch(sourceString);
        }
    }
}
