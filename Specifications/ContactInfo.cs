using Roentgenium.Attributes;
using Roentgenium.FieldGenerators;
using Roentgenium.Interfaces;

namespace Roentgenium.Specifications
{
    [Specification(SpecificationType.ContactInfo)]
    internal class ContactInfoSpecification : ISpecification
    {
        [GeneratorType(typeof(NameFieldGenerator))]
        [GeneratorOptionPair(FieldGeneratorOptionType.Variant, "first")]
        public string FirstName { get; set; }

        [GeneratorType(typeof(NameFieldGenerator))]
        [GeneratorOptionPair(FieldGeneratorOptionType.Variant, "last")]
        public string LastName { get; set; }

        [GeneratorType(typeof(BusinessNameFieldGenerator))]
        public string Company { get; set; }

        [GeneratorType(typeof(AddressFieldGenerator))]
        public string Address1 { get; set; }

        [GeneratorType(typeof(ExtraAddressFieldGenerator))]
        [GeneratorOptionPair(FieldGeneratorOptionType.BlankFrequency, 50)]
        public string Address2 { get; set; }

        [GeneratorType(typeof(CityFieldGenerator))]
        public string City { get; set; }

        [GeneratorType(typeof(StateFieldGenerator))]
        public string State { get; set; }

        [GeneratorType(typeof(ZipCodeGenerator))]
        public string ZipCode { get; set; }

        [GeneratorType(typeof(StringDateTimeFieldGenerator))]
        public string DateOfBirth { get; set; }

        [GeneratorType(typeof(PhoneNumberGenerator))]
        public string Phone { get; set; }

        [GeneratorType(typeof(EmailAddressGenerator))]
        public string Email { get; set; }
    }
}
