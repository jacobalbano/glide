using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace GlideTween
{
	internal class GlideInfo
	{
		private FieldInfo field;
		private PropertyInfo prop;
		private TypeCode typeCode;
		
		private object obj;
		
		public float Value
		{
			get
			{
				if (field != null)
				{
					return Convert.ToSingle(field.GetValue(obj));
				}
				else
				{
					return Convert.ToSingle(prop.GetValue(obj, null));
				}
				
			}
			
			set
			{
				if (field != null)
				{
					field.SetValue(obj, Convert.ChangeType(value, typeCode));
				}
				else
				{
					prop.SetValue(obj, Convert.ChangeType(value, typeCode), null);
				}
			}
		}
		
		public GlideInfo(object obj, string property, bool writeRequired = true)
		{
			this.obj = obj;
			
			var type = obj.GetType();
			
			BindingFlags flags =
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance |
				BindingFlags.Static;
			
			field = type.GetField(property, flags);
			prop = type.GetProperty(property, flags);
			
//			field = type.Field(property);
//			prop = type.Property(property);
			
			if (field != null)	//	Using a field
			{
				typeCode = Type.GetTypeCode(field.GetValue(obj).GetType());
			}
			else if (prop != null)	//	Using a property
			{
				if (!prop.CanRead)
				{
					throw new Exception(string.Format("Property '{0}' on object of type {1} has no setter accessor.", prop, type.FullName));
				}
				
				if (!prop.CanWrite && writeRequired)
				{
					throw new Exception(string.Format("Property '{0}' on object of type {1} has no getter accessor.", prop, type.FullName));
				}
				
				typeCode = Type.GetTypeCode(prop.GetValue(obj, null).GetType());
			}
			else
			{
				//	Couldn't find either
				throw new Exception(string.Format("Field or property '{0}' not found on object of type {1}.", prop, type.FullName));
			}
			
			CheckTypeCode(property);
		}
		
		private void CheckTypeCode(string property)
		{
			if (!(typeCode == TypeCode.Int16 	||
		    typeCode == TypeCode.Int32 			||
		    typeCode == TypeCode.Int64 			||
		    typeCode == TypeCode.UInt16 		||
		    typeCode == TypeCode.UInt32			||
		    typeCode == TypeCode.UInt64			||
		    typeCode == TypeCode.Single			||
		    typeCode == TypeCode.Double			))
			{
				throw new InvalidCastException(string.Format("Property or field to tween must be numeric ({0} on {1}.", property, obj.GetType().Name));
			}
		}
	}
}