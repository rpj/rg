using Roentgenium.Attributes;
using Roentgenium.FieldGenerators;
using Roentgenium.Interfaces;

namespace Roentgenium.Specifications
{
    [Specification(SpecificationType.Names)]
    internal class NamesSpecification : ISpecification
    {        
        [GeneratorType(typeof(NameFieldGenerator))]
        [GeneratorOptionPair(FieldGeneratorOptionType.Variant, "first")]
        public string First { get; set; }

        [GeneratorType(typeof(NameFieldGenerator))]
        [GeneratorOptionPair(FieldGeneratorOptionType.Variant, "middle")]
        public string Middle { get; set; }

        [GeneratorType(typeof(NameFieldGenerator))]
        [GeneratorOptionPair(FieldGeneratorOptionType.Variant, "last")]
        public string Last { get; set; }
    }
}