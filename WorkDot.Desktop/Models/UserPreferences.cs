namespace WorkDot.Models
{
    public class UserPreferences
    {
        /// <summary>
        /// Set the direction layout of the docs to RTL or LTR. If true RTL is used
        /// </summary>
        public bool RightToLeft { get; set; }

        /// <summary>
        /// The current dark light mode that is used
        /// </summary>
        public DarkLightMode DarkLightTheme { get; set; }
    }
}
