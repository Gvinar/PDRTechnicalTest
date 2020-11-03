using System.Text.RegularExpressions;

namespace PDR.PatientBooking.Service.Validation
{
    public static class EmailHelper
    {
        private static Regex _emailRegex =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool IsValidEmailAddress(string sourceString)
        {
            if (string.IsNullOrEmpty(sourceString))
            {
                return false;
            }

            return _emailRegex.IsMatch(sourceString);
        }
    }
}
