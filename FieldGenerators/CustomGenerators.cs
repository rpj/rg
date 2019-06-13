using System;
using System.Collections.Generic;
using Roentgenium.Interfaces;
using RandomNameGeneratorLibrary;

namespace Roentgenium.FieldGenerators
{
    public class NameFieldGenerator : IFieldGenerator
    {
        private static readonly List<string> Suffixes = new List<string>()
        {
            "Jr", "Sr", "II", "III", "IV", "Esq", "PhD", "RN", ""
        };

        public object GenerateField(ref FieldGeneratorOptions opts)
        {
            var NameGen = new PersonNameGenerator(GeneratorsStatic.Random);
            switch ((opts.Variant ?? "").ToLower())
            {
                case "first":
                case "f":
                    return NameGen.GenerateRandomFirstName();
                case "middle":
                case "m":
                    return $"{(char)GeneratorsStatic.Random.Next('A', 'Z' + 1)}";
                case "last":
                case "l":
                    return NameGen.GenerateRandomLastName();
                case "suffix":
                case "s":
                    return Suffixes[GeneratorsStatic.Random.Next(Suffixes.Count)];
                default:
                    return NameGen.GenerateRandomFirstAndLastName();
            }
        }
    }

    public class AddressFieldGenerator : IFieldGenerator
    {
        private static List<string> _streetTypes = new List<string>()
        {
            "Lane", "Road", "Rd", "Ln", "Street", "St", "Way", "Place", "Pl", ""
        };

        public object GenerateField(ref FieldGeneratorOptions opts)
        {
            return $"{GeneratorsStatic.Random.Next(10000)} {Words.RandomWord().FirstCharToUpper()}" +
                $"{(GeneratorsStatic.Random.Next(5) == 0 ? "" : " " + Words.RandomWord().FirstCharToUpper())} " +
                $"{_streetTypes[GeneratorsStatic.Random.Next(_streetTypes.Count)]}";
        }
    }

    public class ExtraAddressFieldGenerator : IFieldGenerator
    {
        private static List<string> _types = new List<string>()
        {
            "Apt.", "Apartment", "Unit", "Suite", "Room", "Rm."
        };

        public object GenerateField(ref FieldGeneratorOptions opts)
        {
            var randType = _types[GeneratorsStatic.Random.Next(_types.Count)];
            var randChar = GeneratorsStatic.Random.Next(2) == 0 ?
                "" : ((char)GeneratorsStatic.Random.Next('A', 'Z' + 1)).ToString();
            // if randChar is empty, always generate a randNum; otherwise, give it a 50/50 chance
            var randNum = string.IsNullOrEmpty(randChar) || GeneratorsStatic.Random.Next(2) == 0 ?
                GeneratorsStatic.Random.Next(1000).ToString() : "";
            return $"{randType} {randNum}{randChar}";
        }
    }

    public class BusinessNameFieldGenerator : IFieldGenerator
    {
        private static readonly List<string> _suffixes = new List<string>()
        {
            "Inc.", "LLC", "LLP", "Co.", "Ltd.", "Company", "Limited", "Incorporated",
            "Partners", "& Co.", "& Company", "Solutions", "Group", "& Associates",
            ""
        };

        public object GenerateField(ref FieldGeneratorOptions opts)
        {
            return Words.RandomWord().FirstCharToUpper().Replace("'", "") + " " +
                   _suffixes[GeneratorsStatic.Random.Next(_suffixes.Count)];
        }
    }

    public class CityFieldGenerator : IFieldGenerator
    {
        public object GenerateField(ref FieldGeneratorOptions opts)
        {
            var returnValue = GeneratorsStatic.Random.GenerateRandomPlaceName();
            if (opts.LengthLimit > 0)
            {
                // first, try for something shorter rather than just truncating a long name
                // but don't try forever
                var tries = 1<<10;
                while (returnValue.Length > opts.LengthLimit && tries-- > 0)
                    returnValue = GeneratorsStatic.Random.GenerateRandomPlaceName();
            }
            
            // last resort: truncate whatever we have if it's still too long
            return returnValue.Substring(0, 
                opts.LengthLimit > 0 && returnValue.Length > opts.LengthLimit ? 
                    opts.LengthLimit : returnValue.Length);
        }
    }

    public class StateFieldGenerator : IFieldGenerator
    {
        // thanks to https://statetable.com/
        // includes territories, minor outlying islands & military abbreviations
        private static readonly List<string> StateTwoLetterCodes = new List<string>()
        {
            "AL","AK","AZ","AR","CA","CO","CT","DE","FL","GA","HI","ID","IL",
            "IN","IA","KS","KY","LA","ME","MD","MA","MI","MN","MS","MO","MT",
            "NE","NV","NH","NJ","NM","NY","NC","ND","OH","OK","OR","PA","RI",
            "SC","SD","TN","TX","UT","VT","VA","WA","WV","WI","WY","DC","PR",
            "VI","AS","GU","MP"
        };

        public object GenerateField(ref FieldGeneratorOptions opts)
        {
            return StateTwoLetterCodes[GeneratorsStatic.Random.Next(StateTwoLetterCodes.Count)];
        }
    }

    public class TitleGenerator : IFieldGenerator
    {
        private static readonly List<string> Titles = new List<string>()
        {
            "Mr", "Mrs", "Ms", "Miss", "Dr", "Sir", "Dame", "Hon", ""
        };

        public object GenerateField(ref FieldGeneratorOptions opts)
        {
            return Titles[GeneratorsStatic.Random.Next(Titles.Count)];
        }
    }

    public class ZipCodeGenerator : IFieldGenerator
    {
        private static readonly int DefaultZipCodeLength = 5;

        public object GenerateField(ref FieldGeneratorOptions opts)
        {
            int length = opts.LengthLimit == FieldGeneratorOptions.DefaultLength ? DefaultZipCodeLength : opts.LengthLimit;
            return $"{GeneratorsStatic.Random.Next((int)Math.Pow(10, length - 1), (int)Math.Pow(10, length))}";
        }
    }

    public class PhoneNumberGenerator : IFieldGenerator
    {
        public object GenerateField(ref FieldGeneratorOptions opts)
        {
            return $"{GeneratorsStatic.Random.Next(100, 1000)}-{GeneratorsStatic.Random.Next(100, 1000)}-{GeneratorsStatic.Random.Next(1000, 10000)}";
        }
    }

    public class SSNGenerator : IFieldGenerator
    {
        public object GenerateField(ref FieldGeneratorOptions opts)
        {
            return $"{GeneratorsStatic.Random.Next(100, 1000)}{GeneratorsStatic.Random.Next(10, 100)}{GeneratorsStatic.Random.Next(1000, 10000)}";
        }
    }

    public class EmailAddressGenerator : IFieldGenerator
    {
        private static readonly List<string> _tlds = new List<string>()
        {
            "com", "net", "org", "gov", "mil", "co", "edu", "us", "biz"
        };

        public object GenerateField(ref FieldGeneratorOptions opts)
        {
            return $"{Words.RandomSafeCharsWord().ToLower()}" +
                    $"{(GeneratorsStatic.Random.Next(2) == 0 ? "" : GeneratorsStatic.Random.Next(100).ToString())}@" +
                    $"{Words.RandomSafeCharsWord().ToLower()}.{_tlds[GeneratorsStatic.Random.Next(_tlds.Count)]}";
        }
    }

    public class GenderGenerator : IFieldGenerator
    {
        private static readonly List<string> _genders = new List<string>()
        {
            "male", "female", "trans", "transitioning", "non-binary", "non-conforming",
            "fluid", "cis male", "cis female", "other"
        };

        public object GenerateField(ref FieldGeneratorOptions opts)
        {
            return _genders[GeneratorsStatic.Random.Next(_genders.Count)];
        }
    }

    public class StringDateTimeFieldGenerator : DateTimeFieldGenerator
    {
        public static readonly string DefaultFormatString = "MM/dd/yyyy";

        public override object GenerateField(ref FieldGeneratorOptions opts)
        {
            var genDt = (DateTime?)base.GenerateField(ref opts);
            var frmtStr = opts.FormatString ?? DefaultFormatString;
            return genDt == null ? "" : genDt.Value.ToString(frmtStr);
        }
    }

    // generic attributes - proposed for C# 8 - would be much better here!
    // something like [DefaultValue<bool>(true)]
    public class BoolAlwaysTrueGenerator : IFieldGenerator
    {
        public object GenerateField(ref FieldGeneratorOptions fo) { return true; }
    }
}
