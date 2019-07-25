using System;
using Roentgenium.Attributes;
using Roentgenium.Interfaces;

namespace Roentgenium.FieldGenerators
{
    [DefaultGeneratorForType(typeof(int))]
    [DefaultGeneratorForType(typeof(int?))]
    [DefaultGeneratorForType(typeof(long))]
    [DefaultGeneratorForType(typeof(long?))]
    public class IntegerFieldGenerator : IFieldGenerator
    {
        public virtual object GenerateField(ref FieldGeneratorOptions opts)
        {
            var genVal = GeneratorsStatic.Random.Next((int)opts.MinValue, (int)opts.MaxValue);

            if (opts.LengthLimit > 0)
            {
                var limVal = Math.Pow(10, opts.LengthLimit);
                while (genVal >= limVal)
                {
                    genVal /= 10;
                }
            }

            return genVal;
        }
    }

    [DefaultGeneratorForType(typeof(uint))]
    [DefaultGeneratorForType(typeof(uint?))]
    [DefaultGeneratorForType(typeof(ulong))]
    [DefaultGeneratorForType(typeof(ulong?))]
    public class UnsignedIntegerFieldGenerator : IFieldGenerator
    {
        public virtual object GenerateField(ref FieldGeneratorOptions opts)
        {
            return GeneratorsStatic.Random.Next(0, (int)opts.MaxValue);
        }
    }

    [DefaultGeneratorForType(typeof(float))]
    [DefaultGeneratorForType(typeof(float?))]
    [DefaultGeneratorForType(typeof(double))]
    [DefaultGeneratorForType(typeof(double?))]
    public class FloatingPointFieldGenerator : IFieldGenerator
    {
        public object GenerateField(ref FieldGeneratorOptions opts)
        {
            return Math.Round(GeneratorsStatic.Random.NextDouble(), opts.RoundTo);
        }
    }

    [DefaultGeneratorForType(typeof(decimal))]
    [DefaultGeneratorForType(typeof(decimal?))]
    public class DecimalPointFieldGenerator : IFieldGenerator
    {
        public object GenerateField(ref FieldGeneratorOptions opts)
        {
            return Math.Round((decimal)(GeneratorsStatic.Random.NextDouble() * opts.MaxValue), opts.RoundTo);
        }
    }

    [DefaultGeneratorForType(typeof(bool))]
    [DefaultGeneratorForType(typeof(bool?))]
    public class BooleanFieldGenerator : IFieldGenerator
    {
        public object GenerateField(ref FieldGeneratorOptions options)
        {
            return GeneratorsStatic.Random.Next() % 2 == 0;
        }
    }

    [DefaultGeneratorForType(typeof(DateTime))]
    [DefaultGeneratorForType(typeof(DateTime?))]
    public class DateTimeFieldGenerator : IFieldGenerator
    {
        public virtual object GenerateField(ref FieldGeneratorOptions options)
        {
            return DateTime.Now - new TimeSpan(0, 0, GeneratorsStatic.Random.Next());
        }
    }

    [DefaultGeneratorForType(typeof(string))]
    public class StringFieldGenerator : IFieldGenerator
    {
        public object GenerateField(ref FieldGeneratorOptions opts)
        {
            var retStr = "";
            if (opts.IsNumeric || opts.AllowUnsafeChars)
            {
                // printable ASCII character range sourced from:
                // http://facweb.cs.depaul.edu/sjost/it212/documents/ascii-pr.htm
                var lowerLim = opts.IsNumeric ? '0' : '!';
                var upperLim = opts.IsNumeric ? '9' : '~';

                var randLength = GeneratorsStatic.Random.Next(1, opts.LengthLimit);
                for (int i = 0; i < randLength; i++)
                    retStr += (char)(GeneratorsStatic.Random.Next(lowerLim, upperLim + 1));
            }
            else
                retStr = Words.RandomSafeCharsWordsOfLength(opts.LengthLimit);

            // can happen in the RandomSafeCharsWordsOfLength path
            if (retStr.Length > opts.LengthLimit)
                retStr = retStr.Substring(0, opts.LengthLimit);

            return retStr;
        }
    }

    [DefaultGeneratorForType(typeof(Guid))]
    [DefaultGeneratorForType(typeof(Guid?))]
    public class GuidFieldGenerator: IFieldGenerator
    {
        public object GenerateField(ref FieldGeneratorOptions opts)
        {
            return Guid.NewGuid();
        }
    }
}
