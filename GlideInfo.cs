using System;
using System.Collections.Generic;
using System.Reflection;

namespace Glide
{
	internal class GlideInfo
	{
		static GlideInfo()
		{
			numericTypes = new Type[] {
				typeof(Int16),
				typeof(Int32),
				typeof(Int64),
				typeof(UInt16),
				typeof(UInt32),
				typeof(UInt64),
				typeof(Single),
				typeof(Double)
			};
			
			flags =
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance |
				BindingFlags.Static;
		}
		
		private static Type[] numericTypes;
		private static BindingFlags flags;
		
		private FieldInfo field;
		private PropertyInfo prop;
		
		private object Target;
		
		public string Name { get; private set; }
		
		public object Value
		{
			get { return field != null ? field.GetValue(Target) : prop.GetValue(Target, null); }
			set {
				if (field != null) 
					field.SetValue(Target, value);
				else
					prop.SetValue(Target, value, null);
			}
		}
		
		public GlideInfo(object Target, string property, bool writeRequired = true)
		{
			this.Target = Target;
			Name = property;
			
			var type = Target.GetType();
			field = type.GetField(property, flags);
			prop = type.GetProperty(property, flags);
			
			if (field == null)
			{
				if (prop == null)
				{
					//	Couldn't find either
					throw new Exception(string.Format("Field or property '{0}' not found on object of type {1}.", property, type.FullName));
				}
				else
				{
					if (!prop.CanRead)
					{
						throw new Exception(string.Format("Property '{0}' on object of type {1} has no setter accessor.", prop, type.FullName));
					}
					
					if (!prop.CanWrite && writeRequired)
					{
						throw new Exception(string.Format("Property '{0}' on object of type {1} has no getter accessor.", prop, type.FullName));
					}
				}
			}
			
			CheckPropertyType(Value.GetType(), property, Target.GetType().Name);
		}
		
		private void CheckPropertyType(Type type, string prop, string targetTypeName)
		{
			if (!ValidatePropertyType(type))
			{
				throw new InvalidCastException(string.Format("Property is invalid: ({0} on {1}).", prop, targetTypeName));
			}
		}
		
		protected virtual bool ValidatePropertyType(Type type)
		{
			return AnyEquals(type, numericTypes);
		}
		
		static bool AnyEquals<T>(T value, params T[] options)
		{
			foreach (var option in options)
				if (value.Equals(option)) return true;
			
			return false;
		}
	}
}
