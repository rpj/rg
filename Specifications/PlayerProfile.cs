using Roentgenium.Attributes;
using Roentgenium.FieldGenerators;
using Roentgenium.Interfaces;
using System;

namespace Roentgenium.Specifications
{
    [Specification(SpecificationType.PlayerProfile)]
    internal class PlayerProfileSpecification : ISpecification
    {
        public Guid Id { get; set; }
        
        [GeneratorType(typeof(TitleGenerator))]
        public string Title { get; set; }

        [GeneratorType(typeof(NameFieldGenerator))]
        [GeneratorOptionPair(FieldGeneratorOptionType.Variant, "first")]
        public string FirstName { get; set; }

        [GeneratorType(typeof(NameFieldGenerator))]
        [GeneratorOptionPair(FieldGeneratorOptionType.Variant, "middle")]
        public string MiddleInitial { get; set; }

        [GeneratorType(typeof(NameFieldGenerator))]
        [GeneratorOptionPair(FieldGeneratorOptionType.Variant, "last")]
        public string LastName { get; set; }

        [GeneratorType(typeof(NameFieldGenerator))]
        [GeneratorOptionPair(FieldGeneratorOptionType.Variant, "suffix")]
        public string NameSuffix { get; set; }

        [GeneratorType(typeof(BusinessNameFieldGenerator))]
        public string Company { get; set; }

        [GeneratorType(typeof(AddressFieldGenerator))]
        public string StreetAddress1 { get; set; }
        
        [GeneratorType(typeof(ExtraAddressFieldGenerator))]
        [GeneratorOptionPair(FieldGeneratorOptionType.BlankFrequency, 50)]
        public string StreetAddress2 { get; set; }
        
        [GeneratorType(typeof(CityFieldGenerator))]
        public string City { get; set; }
        
        [GeneratorType(typeof(StateFieldGenerator))]
        public string State { get; set; }

        [GeneratorType(typeof(ZipCodeGenerator))]
        public string ZipCode { get; set; }
        
        [GeneratorType(typeof(StringDateTimeFieldGenerator))]
        public string DateOfBirth { get; set; }

        [GeneratorType(typeof(PhoneNumberGenerator))]
        public string HomePhoneNumber { get; set; }

        [GeneratorType(typeof(PhoneNumberGenerator))]
        public string MobilePhoneNumber { get; set; }

        [GeneratorType(typeof(EmailAddressGenerator))]
        public string EmailAddress { get; set; }

        [GeneratorType(typeof(SSNGenerator))]
        public string PlayerId { get; set; }

        [GeneratorOptionPair(FieldGeneratorOptionType.AllowUnsafeChars, true)]
        [GeneratorOptionPair(FieldGeneratorOptionType.LengthLimit, 32)]
        public string Password { get; set; }
        
        [GeneratorOptionPair(FieldGeneratorOptionType.MinValue, 500)]
        [GeneratorOptionPair(FieldGeneratorOptionType.MaxValue, 5000000)]
        public int CurrentCredits { get; set; }

        [GeneratorOptionPair(FieldGeneratorOptionType.BlankFrequency, 30)]
        public string MemberName { get; set; }
        
        [GeneratorType(typeof(StringDateTimeFieldGenerator))]
        public string MemberJoinDate { get; set; }
        
        [GeneratorType(typeof(StringDateTimeFieldGenerator))]
        public string MemberStartDate { get; set; }
        
        [GeneratorType(typeof(StringDateTimeFieldGenerator))]
        [GeneratorOptionPair(FieldGeneratorOptionType.BlankFrequency, 95)]
        public string MemberQuitDate { get; set; }

        [GeneratorOptionPair(FieldGeneratorOptionType.MinValue, 18)]
        [GeneratorOptionPair(FieldGeneratorOptionType.MaxValue, 100)]
        [FieldLinkerType("DateOfBirth", typeof(IntegerYearsSinceDateStringLinker))]
        public int MemberAge { get; set; }

        [GeneratorOptionPair(FieldGeneratorOptionType.LengthLimit, 6)]
        [GeneratorOptionPair(FieldGeneratorOptionType.IsNumeric, true)]
        [GeneratorOptionPair(FieldGeneratorOptionType.BlankFrequency, 40)]
        public string LinkedShortCode { get; set; }

        [SequenceNumber]
        public uint SequenceNumber { get; set; }
    }
}
