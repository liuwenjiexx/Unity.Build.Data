using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace System.Configure
{
    public abstract class EnumableAttribute : Attribute
    {
        public bool IsMask { get; set; }
        public string Separator { get; set; }
        public abstract object[] GetValues();

        public abstract string[] GetDisplayTexts();
    }


    public class EnumValuesAttribute : EnumableAttribute
    {
        public EnumValuesAttribute(object[] values)
        {
            this.Values = values;
        }
        public EnumValuesAttribute(object[] values, string[] displayTexts)
        {
            this.Values = values;
            this.DisplayTexts = displayTexts;
        }

        public object[] Values { get; set; }

        public string[] DisplayTexts { get; set; }



        public override object[] GetValues()
        {
            return Values;
        }

        public override string[] GetDisplayTexts()
        {
            return DisplayTexts;
        }

    }




}