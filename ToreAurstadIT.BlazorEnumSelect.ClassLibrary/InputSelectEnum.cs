// file: Shared/InputSelectEnum.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace ToreAurstadIT.BlazorEnumSelect
{
    /// <summary>
    // Enum select control which supports generation of options from enum type of data bound property. 
    /// Based upon: https://www.meziantou.net/creating-a-inputselect-component-for-enumerations-in-blazor.htm
    // with some additional features. This sample also supports nullable enumerable enum type.
    // Class is now not sealed in case you want to adapt the enum control more.
    // The numeric value is sorted ascending numerically
    // Parameter AdditionalCssClasses can be set to a string to customize the css class(es) of the select. E.g. Blazorise uses "custom-select". It is possible to add multiple CSS classes here which will be added to those that InputSelect base class also adds.
    // Parameter ShowIntValues to show enum value also in the text of option which defaults to true
    // Parameter EmptyTextValue will if set to non null value will check if the int value is equal to this set empty text for the option element
    // Inherit from InputBase so the hard work is already implemented 
    // Note that adding a constraint on TEnum (where T : Enum) doesn't work when used in the view, Razor raises an error at build time. Also, this would prevent using nullable types...
    /// </summary>
    /// <typeparam name="TEnum">Type of enum</typeparam>
    public class InputSelectEnum<TEnum> : InputBase<TEnum>
    {

        [Parameter]
        public bool ShowIntValues { get; set; } = true;

        [Parameter]
        public int? EmptyTextValue { get; set; }

        [Parameter]
        public string AdditionalCssClasses { get; set; }

        private List<object> EnumValuesSortedNumerically = new List<object>();


        // Generate html when the component is rendered.
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "select");
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            string compoundCssClass = !string.IsNullOrWhiteSpace(AdditionalCssClasses) ? $"{AdditionalCssClasses} {CssClass}" : CssClass;
            builder.AddAttribute(2, "class", compoundCssClass);
            builder.AddAttribute(3, "value", BindConverter.FormatValue(CurrentValueAsString));
            builder.AddAttribute(4, "onchange", EventCallback.Factory.CreateBinder<string>(this, value => CurrentValueAsString = value, CurrentValueAsString, null));

            // Add an option element per enum value
            var enumType = GetEnumType();
            BuildEnumValuesSorted(enumType);

            foreach (TEnum value in EnumValuesSortedNumerically)
            {
                builder.OpenElement(5, "option");
                builder.AddAttribute(6, "value", value.ToString());

                string enumValuePrefix = this.ShowIntValues ? $"{Convert.ToInt32(value)} : " : string.Empty;

                if (EmptyTextValue.HasValue && Convert.ToInt32(value) == EmptyTextValue.Value)
                {
                    builder.AddContent(7, string.Empty);
                }
                else
                {
                    builder.AddContent(7, enumValuePrefix + GetDisplayName(value));
                }
                builder.CloseElement();
            }

            builder.CloseElement(); // close the select element
        }

        protected override bool TryParseValueFromString(string value, out TEnum result, out string validationErrorMessage)
        {
            // Let's Blazor convert the value for us 😊
            if (BindConverter.TryConvertTo(value, CultureInfo.CurrentCulture, out TEnum parsedValue))
            {
                result = parsedValue;
                validationErrorMessage = null;
                return true;
            }

            // Map null/empty value to null if the bound object is nullable
            if (string.IsNullOrEmpty(value))
            {
                var nullableType = Nullable.GetUnderlyingType(typeof(TEnum));
                if (nullableType != null)
                {
                    result = default;
                    validationErrorMessage = null;
                    return true;
                }
            }

            // The value is invalid => set the error message
            result = default;
            validationErrorMessage = $"The {FieldIdentifier.FieldName} field is not valid.";
            return false;
        }

        private void BuildEnumValuesSorted(Type t)
        {
            List<object> temp = new List<object>();
            foreach (var enumValue in t.GetEnumValues())
            {
                temp.Add(enumValue);
            }
            EnumValuesSortedNumerically = temp.OrderBy(t => (int)t).ToList();
        }

        // Get the display text for an enum value:
        // - Use the DisplayAttribute if set on the enum member, so this support localization
        // - Fallback on Humanizer to decamelize the enum member name
        private string GetDisplayName(TEnum value)
        {
            // Read the Display attribute name
            var member = value.GetType().GetMember(value.ToString())[0];
            var displayAttribute = member.GetCustomAttribute<DisplayAttribute>();
            if (displayAttribute != null)
                return displayAttribute.GetName();

            // Require the NuGet package Humanizer.Core
            // <PackageReference Include = "Humanizer.Core" Version = "2.8.26" />
            return value.ToString().Humanize();
        }

        // Get the actual enum type. It unwrap Nullable<T> if needed
        // MyEnum  => MyEnum
        // MyEnum? => MyEnum
        private Type GetEnumType()
        {
            var nullableType = Nullable.GetUnderlyingType(typeof(TEnum));
            if (nullableType != null)
                return nullableType;

            return typeof(TEnum);
        }
    }


}