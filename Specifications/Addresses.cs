using Roentgenium.Attributes;
using Roentgenium.FieldGenerators;
using Roentgenium.Interfaces;

namespace Roentgenium.Specifications
{
    [Specification(SpecificationType.Addresses)]
    internal class AddressesSpecification : ISpecification
    {
        [GeneratorType(typeof(AddressFieldGenerator))]
        public string Address { get; set; }
        
        [GeneratorType(typeof(ExtraAddressFieldGenerator))]
        [GeneratorOptionPair(FieldGeneratorOptionType.BlankFrequency, 50)]
        public string Extra { get; set; }
        
        [GeneratorType(typeof(CityFieldGenerator))]
        public string City { get; set; }
        
        [GeneratorType(typeof(StateFieldGenerator))]
        public string State { get; set; }

        [GeneratorType(typeof(ZipCodeGenerator))]
        public string Zip { get; set; }
    }
}