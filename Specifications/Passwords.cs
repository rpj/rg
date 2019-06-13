using Roentgenium.Attributes;
using Roentgenium.Interfaces;

namespace Roentgenium.Specifications
{
    [Specification(SpecificationType.Passwords)]
    internal class PasswordsSpecification : ISpecification
    {
        [GeneratorOptionPair(FieldGeneratorOptionType.AllowUnsafeChars, true)]
        [GeneratorOptionPair(FieldGeneratorOptionType.LengthLimit, 32)]
        public string Password { get; set; }
    }
}