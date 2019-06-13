using System;
using Roentgenium.Attributes;
using Roentgenium.FieldGenerators;
using Roentgenium.Interfaces;
using Roentgenium.Stages.Sinks;

namespace Roentgenium.Specifications
{
    [Specification(SpecificationType.CensusData)]
    internal class CensusDataSpecification : ISpecification
    {
        [SequenceNumber]
        [FormatFieldOutputSpec(typeof(CsvSink), "seq id")]
        public ulong UniqueIdentifier { get; set; }

        [FormatFieldOutputSpec(typeof(CsvSink), "track id")]
        public Guid TrackingId { get; set; }

        [GeneratorType(typeof(TitleGenerator))]
        [FormatFieldOutputSpec(typeof(CsvSink), "title")] 
        public string Title { get; set; }
        
        [GeneratorType(typeof(NameFieldGenerator))]
        [GeneratorOptionPair(FieldGeneratorOptionType.Variant, "first")]
        [FormatFieldOutputSpec(typeof(CsvSink), "first")]
        public string FirstName { get; set; }

        [GeneratorType(typeof(NameFieldGenerator))]
        [GeneratorOptionPair(FieldGeneratorOptionType.Variant, "middle")]
        [FormatFieldOutputSpec(typeof(CsvSink), "middle")]
        public string MiddleName { get; set; }

        [GeneratorType(typeof(NameFieldGenerator))]
        [GeneratorOptionPair(FieldGeneratorOptionType.Variant, "last")]
        [FormatFieldOutputSpec(typeof(CsvSink), "last")]
        public string LastName { get; set; }

        [GeneratorType(typeof(NameFieldGenerator))]
        [GeneratorOptionPair(FieldGeneratorOptionType.Variant, "suffix")]
        [FormatFieldOutputSpec(typeof(CsvSink), "suf")] 
        public string Suffix { get; set; }

        [GeneratorType(typeof(BusinessNameFieldGenerator))]
        [FormatFieldOutputSpec(typeof(CsvSink), "business name")] 
        public string BusinessName { get; set; }
        
        [GeneratorType(typeof(AddressFieldGenerator))]
        [FormatFieldOutputSpec(typeof(CsvSink), "street 1")] 
        public string Address1 { get; set; }

        [GeneratorType(typeof(AddressFieldGenerator))]
        [FormatFieldOutputSpec(typeof(CsvSink), "street 2")] 
        public string Address2 { get; set; }
        
        [GeneratorType(typeof(ExtraAddressFieldGenerator))]
        [FormatFieldOutputSpec(typeof(CsvSink), "extra address")] 
        [GeneratorOptionPair(FieldGeneratorOptionType.BlankFrequency, 80)]
        public string ExtraAddress { get; set; }

        [GeneratorType(typeof(CityFieldGenerator))]
        [FormatFieldOutputSpec(typeof(CsvSink), "city")]
        public string City { get; set; }
        
        [GeneratorType(typeof(StateFieldGenerator))]
        [FormatFieldOutputSpec(typeof(CsvSink), "state")]
        public string State { get; set; }

        [GeneratorType(typeof(ZipCodeGenerator))]
        [FormatFieldOutputSpec(typeof(CsvSink), "zip5")]
        public string Zip5 { get; set; }

        [GeneratorType(typeof(ZipCodeGenerator))]
        [FormatFieldOutputSpec(typeof(CsvSink), "zip4")] 
        [GeneratorOptionPair(FieldGeneratorOptionType.LengthLimit, 4)]
        public string Zip4 { get; set; }
        
        [GeneratorType(typeof(StringDateTimeFieldGenerator))]
        [FormatFieldOutputSpec(typeof(CsvSink), "birth date")] 

        public string BirthDate { get; set; }
        
        [GeneratorType(typeof(StringDateTimeFieldGenerator))]
        [FormatFieldOutputSpec(typeof(CsvSink), "death date")] 
        [GeneratorOptionPair(FieldGeneratorOptionType.BlankFrequency, 95)]
        public string DeathDate { get; set; }

        [GeneratorType(typeof(EmailAddressGenerator))]
        [FormatFieldOutputSpec(typeof(CsvSink), "email address")] 
        public string EmailAddress { get; set; }

        [GeneratorType(typeof(SSNGenerator))]
        [FormatFieldOutputSpec(typeof(CsvSink), "ssn")] 

        public string SSN { get; set; }

        [FormatFieldOutputSpec(typeof(CsvSink), "age")]
        [GeneratorOptionPair(FieldGeneratorOptionType.MinValue, 18)]
        [GeneratorOptionPair(FieldGeneratorOptionType.MaxValue, 100)]
        [FieldLinkerType("BirthDate", typeof(IntegerYearsSinceDateStringLinker))]
        public int Age { get; set; }

        [FormatFieldOutputSpec(typeof(CsvSink), "income")] 
        [GeneratorOptionPair(FieldGeneratorOptionType.MinValue, 5000)]
        [GeneratorOptionPair(FieldGeneratorOptionType.MaxValue, 250000)]
        public int Income { get; set; }

        [FormatFieldOutputSpec(typeof(CsvSink), "gender")] 
        [GeneratorType(typeof(GenderGenerator))]
        [GeneratorOptionPair(FieldGeneratorOptionType.BlankFrequency, 100)]
        public string Gender { get; set; }

        [FormatFieldOutputSpec(typeof(CsvSink), "employer")]
        [GeneratorType(typeof(BusinessNameFieldGenerator))]
        [GeneratorOptionPair(FieldGeneratorOptionType.BlankFrequency, 30)]
        public string Employer { get; set; }

        [FormatFieldOutputSpec(typeof(CsvSink), "time employed years")]
        [FieldLinkerType("Employer", typeof(TimeEmployedToEmployerLinker))]
        [GeneratorOptionPair(FieldGeneratorOptionType.MinValue, 1)]
        [GeneratorOptionPair(FieldGeneratorOptionType.MaxValue, 40)]
        public int TimeEmployedYears { get; set; }

        [FormatFieldOutputSpec(typeof(CsvSink), "debt to income ratio")] 
        [GeneratorOptionPair(FieldGeneratorOptionType.MinValue, 0.0)]
        [GeneratorOptionPair(FieldGeneratorOptionType.MaxValue, 100.0)]
        public decimal DebtToIncomeRatio { get; set; }

        [FormatFieldOutputSpec(typeof(CsvSink), "has secondary income")] 
        public bool HasSecondaryIncome { get; set; }

        [FormatFieldOutputSpec(typeof(CsvSink), "secondary income")]
        [GeneratorOptionPair(FieldGeneratorOptionType.MinValue, 250)]
        [GeneratorOptionPair(FieldGeneratorOptionType.MaxValue, 75000)]
        [FieldLinkerType("HasSecondaryIncome", typeof(EnabledDecimalFieldLinker))]

        public decimal SecondaryIncome { get; set; }

        [GeneratorType(typeof(PhoneNumberGenerator))]
        [FormatFieldOutputSpec(typeof(CsvSink), "phone number")] 
        public string PhoneNumber { get; set; }
    }
}