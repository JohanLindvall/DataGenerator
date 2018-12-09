﻿// Copyright(c) 2017, 2018 Johan Lindvall
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace OrmMock.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Defines reflection utility methods.
    /// </summary>
    public static class ReflectionUtility
    {
        public static IList<PropertyInfo> GetPublicPropertiesWithGetters(Type t) => t.GetProperties().Where(HasGetter).ToList();

        public static IList<PropertyInfo> GetPublicPropertiesWithGettersAndSetters(Type t) => t.GetProperties().Where(p => HasGetter(p) && HasSetter(p)).ToList();

        public static bool HasSetter(PropertyInfo property) => property.SetMethod != null;

        public static bool HasGetter(PropertyInfo property) => property.GetMethod != null;

        /// <summary>
        /// Returns true if the property is a nullable value type or string.
        /// </summary>
        /// <param name="property">The property to check.</param>
        /// <returns>True if the property is a nullable value type or string, false otherwise.</returns>
        public static bool IsNullableOrString(PropertyInfo property) => Nullable.GetUnderlyingType(property.PropertyType) != null || property.PropertyType == typeof(string);

        /// <summary>
        /// Returns true if the property type is a value type, or a nullable value type or string.
        /// </summary>
        /// <param name="property">The property to check.</param>
        /// <returns>True if the property type is value type, false otherwise.</returns>
        public static bool IsValueTypeOrNullableOrString(PropertyInfo property) => property.PropertyType.IsValueType || ReflectionUtility.IsNullableOrString(property);

        /// <summary>
        /// Returns true if the property type is a non-generic reference type (but not nullable or string)
        /// </summary>
        /// <param name="property">The property to check.</param>
        /// <returns>True if the property type is reference type, false otherwise.</returns>
        public static bool IsNonGenericReferenceType(PropertyInfo property) => property.PropertyType.IsClass && !property.PropertyType.IsGenericType && !ReflectionUtility.IsNullableOrString(property);
    }
}
