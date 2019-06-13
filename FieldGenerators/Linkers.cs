﻿using System;
using Roentgenium.Interfaces;

namespace Roentgenium
{
    public class FieldDuplicatorLinker : IFieldLinker
    {
        public object LinkField(object linkedFieldValue, ref FieldGeneratorOptions opts)
        {
            return linkedFieldValue;
        }
    }

    public class IntegerYearsSinceDateStringLinker : IFieldLinker
    {
        public object LinkField(object linkedDateStr, ref FieldGeneratorOptions opts)
        {
            if (DateTime.TryParse((string)linkedDateStr, out var outDate))
                return (int)(DateTime.UtcNow - outDate).TotalDays / 365;

            throw new InvalidProgramException("Bad specification!");
        }
    }

    public class TimeEmployedToEmployerLinker : IFieldLinker
    {
        public object LinkField(object linkedEmployer, ref FieldGeneratorOptions opts)
        {
            return string.IsNullOrEmpty((string)linkedEmployer) ? 0 : 
                new Random().Next(Math.Min((int)opts.MaxValue, 40));
        }
    }
    
    // TODO: would be GREAT to use generics for linkers...
    public class EnabledIntegerFieldLinker : IFieldLinker
    {
        public virtual object LinkField(object linked, ref FieldGeneratorOptions opts)
        {
            return (bool)linked ? new Random().Next((int)opts.MaxValue) : 0;
        }
    }
    
    // TODO: would be GREAT to use generics for linkers...
    public class EnabledDecimalFieldLinker : EnabledIntegerFieldLinker
    {
        public override object LinkField(object linkedBool, ref FieldGeneratorOptions opts)
        {
            return new decimal((int)base.LinkField(linkedBool, ref opts));
        }
    }
}
