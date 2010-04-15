//
// Copyright (C) 2010 Novell Inc. http://novell.com
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Markup;
using System.Xaml.Schema;

namespace System.Xaml
{
	static class TypeExtensionMethods
	{
		// FIXME: this likely needs to be replaced with XamlTypeName
		public static string GetXamlName (this Type type)
		{
			if (!type.IsNested)
				return type.Name;
			return type.DeclaringType.GetXamlName () + "+" + type.Name;
		}

		#region inheritance search and custom attribute provision

		public static T GetCustomAttribute<T> (this ICustomAttributeProvider type, bool inherit) where T : Attribute
		{
			foreach (var a in type.GetCustomAttributes (typeof (T), inherit))
				return (T) (object) a;
			return null;
		}

		public static T GetCustomAttribute<T> (this XamlType type) where T : Attribute
		{
			if (type.UnderlyingType == null)
				return null;

			T ret = type.CustomAttributeProvider.GetCustomAttribute<T> (true);
			if (ret != null)
				return ret;
			if (type.BaseType != null)
				return type.BaseType.GetCustomAttribute<T> ();
			return null;
		}

		public static bool ImplementsAnyInterfacesOf (this Type type, params Type [] definitions)
		{
			return definitions.Any (t => ImplementsInterface (type, t));
		}

		public static bool ImplementsInterface (this Type type, Type definition)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
			if (definition == null)
				throw new ArgumentNullException ("definition");

			foreach (var iface in type.GetInterfaces ())
				if (iface == definition || (iface.IsGenericType && iface.GetGenericTypeDefinition () == definition))
					return true;
			return false;
		}
		
		#endregion
		
		#region type conversion and member value retrieval
		
		public static object GetStringValue (this XamlType xt, object obj)
		{
			if (obj == null)
				return String.Empty;
			if (obj is DateTime)
				// FIXME: DateTimeValueSerializer should apply
				return TypeDescriptor.GetConverter (typeof (DateTime)).ConvertToInvariantString (obj);
			else
				return xt.ConvertObject (obj, typeof (string));
		}

		public static object ConvertObject (this XamlType xt, object target, Type explicitTargetType)
		{
			return DoConvert (xt.TypeConverter, target, explicitTargetType ?? xt.UnderlyingType);
		}
		
		public static object GetMemberValue (this XamlMember xm, object target)
		{
			object native = GetPropertyOrFieldValue (xm, target);
			var targetRType = xm.TargetType == null ? null : xm.TargetType.UnderlyingType;
			return DoConvert (xm.TypeConverter, native, targetRType);
		}
		
		static object DoConvert (XamlValueConverter<TypeConverter> converter, object value, Type targetType)
		{
			// First get member value, then convert it to appropriate target type.
			var tc = converter != null ? converter.ConverterInstance : null;
			if (tc != null && targetType != null && tc.CanConvertTo (targetType))
				return tc.ConvertTo (value, targetType);
			return value;
		}

		static object GetPropertyOrFieldValue (this XamlMember xm, object target)
		{
			// FIXME: should this be done here??
			if (xm == XamlLanguage.Initialization)
				return target;

			var mi = xm.UnderlyingMember;
			var fi = mi as FieldInfo;
			if (fi != null)
				return fi.GetValue (target);
			var pi = mi as PropertyInfo;
			if (pi != null)
				return ((PropertyInfo) mi).GetValue (target, null);

			throw new NotImplementedException ();
		}
		
		#endregion

		public static IEnumerable<XamlMember> GetAllReadWriteMembers (this XamlType type)
		{
			if (type != XamlLanguage.Null) // FIXME: probably by different condition
				yield return XamlLanguage.Initialization;
			foreach (var m in type.GetAllMembers ())
				yield return m;
		}

		public static bool ListEquals (this IList<XamlType> a1, IList<XamlType> a2)
		{
			if (a1 == null || a1.Count == 0)
				return a2 == null || a2.Count == 0;
			if (a2 == null || a2.Count == 0)
				return false;
			if (a1.Count != a2.Count)
				return false;
			for (int i = 0; i < a1.Count; i++)
				if (a1 [i] != a2 [i])
					return false;
			return true;
		}
	}
}