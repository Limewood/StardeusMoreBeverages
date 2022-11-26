using KL.Utils;
using System;
using System.Reflection;

namespace MoreBeverages.Utils {
    public sealed class ReflectionUtils {
        public static void SetValue(Type type, object obj, string fieldName, object value) {
            try {
        	    FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                field?.SetValue(obj, value);
            } catch(Exception e) {
                D.Warn("Not able to get field {0}: {1}", fieldName, e.Message);
            }
        }

        public static object GetValue(Type type, object obj, string fieldName) {
            try {
        	    FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                return field.GetValue(obj);
            } catch(Exception e) {
                D.Warn("Not able to get field {0}: {1}", fieldName, e.Message);
                return null;
            }
        }

        public static object GetPropertyValue(Type type, object obj, string propertyName, object[] paramsArray = null) {
            try {
        	    PropertyInfo property = type.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                return property.GetValue(obj, paramsArray);
            } catch(Exception e) {
                D.Warn("Not able to get property {0}: {1}", propertyName, e.Message);
                return null;
            }
        }

        public static object CallMethod(Type type, object obj, string methodName, object[] paramsArray = null) {
            try {
                MethodInfo method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                return method.Invoke(obj, paramsArray);
            } catch(Exception e) {
                D.Warn("Not able to call method {0}: {1}", methodName, e.Message);
                return null;
            }
        }
    }
}