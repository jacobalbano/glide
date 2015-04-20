using System;
using System.Collections.Generic;
using System.Reflection;

namespace Glide
{
	internal class GlideInfo
	{
		private static BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
		
		public string PropertyName { get; private set; }
		public Type PropertyType { get; private set; }
		
		private FieldInfo field;
		private PropertyInfo prop;
		private object Target;
		
		public object Value
		{
			get
			{
				return field != null ?
					field.GetValue(Target) :
					prop.GetValue(Target, null);
			}
			
			set
			{
				if (field != null) field.SetValue(Target, value);
				else prop.SetValue(Target, value, null);
			}
		}
		
		public GlideInfo(object Target, string property, bool writeRequired = true)
		{
			this.Target = Target;
			PropertyName = property;
			
			var targetType = Target.GetType() == typeof(Type) ? (Type) Target : Target.GetType();
			
			field = targetType.GetField(property, flags);
			prop = targetType.GetProperty(property, flags);
			
			if (field != null)
			{
				PropertyType = field.FieldType;
			}
			else if (prop != null)
			{
				PropertyType = prop.PropertyType;
			}
			else
			{
				//	Couldn't find either
				throw new Exception(string.Format("Field or {0} property '{1}' not found on object of type {2}.",
					writeRequired ? "read/write" : "readable",
					property, targetType.FullName));
			}
		}
	}
}
