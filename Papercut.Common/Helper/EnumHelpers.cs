namespace Papercut.Common.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class EnumHelpers
    {
        /// <summary>
        ///     Gets a enum as a list
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <returns></returns>
        public static List<TEnum> GetEnumList<TEnum>()
            where TEnum : struct
        {
            Type enumType = typeof(TEnum);

            // Can't use type constraints on value types, so have to do check like this
            if (enumType.BaseType != typeof(Enum))
                throw new ArgumentException("EnumAsList does not support non-enum types");

            Array enumValArray = Enum.GetValues(enumType);

            var enumValues = new List<TEnum>(enumValArray.Length);

            enumValues.AddRange(
                enumValArray.Cast<int>().Select(val => (TEnum)Enum.Parse(enumType, val.ToString())));

            return enumValues;
        }

        /// <summary>
        /// Enum to names
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <returns></returns>
        public static List<string> GetNames<TEnum>()
            where TEnum : struct
        {
            Type enumType = typeof(TEnum);

            // Can't use type constraints on value types, so have to do check like this
            if (enumType.BaseType != typeof(Enum)) throw new ArgumentException("EnumToNames does not support non-enum types");

            return Enum.GetNames(enumType).ToList();
        }
    }
}