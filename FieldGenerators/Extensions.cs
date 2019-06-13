using System.Linq;

namespace Roentgenium.FieldGenerators
{
    public static class FieldGeneratorStringExtensions
    {
        public static string FirstCharToUpper(this string input)
        {
            switch (input)
            {
                case null: case "": return "";
                default: return input.First().ToString().ToUpper() + input.Substring(1);
            }
        }
    }
}
