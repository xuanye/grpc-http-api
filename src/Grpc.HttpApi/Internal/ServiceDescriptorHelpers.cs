using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;

namespace Grpc.HttpApi.Internal
{
    internal static class ServiceDescriptorHelpers
    {
        public static ServiceDescriptor GetServiceDescriptor(Type serviceReflectionType)
        {
            var property = serviceReflectionType.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static);
            if (property != null)
            {
                return (ServiceDescriptor)property.GetValue(null);
            }

            throw new InvalidOperationException($"Get not find Descriptor property on {serviceReflectionType.Name}.");
        }

        public static bool TryResolveDescriptors(MessageDescriptor messageDescriptor, string variable, [NotNullWhen(true)]out List<FieldDescriptor> fieldDescriptors)
        {
            fieldDescriptors = null;
            var path = variable.AsSpan();
            var currentDescriptor = messageDescriptor;

            while (path.Length > 0)
            {
                var separator = path.IndexOf('.');

                string fieldName;
                if (separator != -1)
                {
                    fieldName = path.Slice(0, separator).ToString();
                    path = path.Slice(separator + 1);
                }
                else
                {
                    fieldName = path.ToString();
                    path = ReadOnlySpan<char>.Empty;
                }
               
                var field = currentDescriptor?.FindFieldByName(fieldName);
                if (field == null)
                {
                    //cast "jsonName" to ¡°json_name¡±;
                    var otherFiledName = Regex.Replace(fieldName, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
                    field = currentDescriptor?.FindFieldByName(otherFiledName);                    
                }
                if(field == null)
                {
                    fieldDescriptors = null;
                    return false;
                }

                fieldDescriptors ??= new List<FieldDescriptor>();

                fieldDescriptors.Add(field);
                
                currentDescriptor = field.FieldType == FieldType.Message ? field.MessageType : null;

            }

            return fieldDescriptors != null;
        }

        private static object ConvertValue(object value, FieldDescriptor descriptor)
        {
            string sNumValue="0";
            string sBoolValue = "false";

            if(descriptor.FieldType == FieldType.Double
                || descriptor.FieldType == FieldType.Float
                 || descriptor.FieldType == FieldType.Int64
                 || descriptor.FieldType == FieldType.SInt64
                 || descriptor.FieldType == FieldType.SFixed64
                 || descriptor.FieldType == FieldType.UInt64
                 || descriptor.FieldType == FieldType.Fixed64
                 || descriptor.FieldType == FieldType.Int32
                 || descriptor.FieldType == FieldType.SInt32
                 || descriptor.FieldType == FieldType.SFixed32
                 || descriptor.FieldType == FieldType.UInt32
                 || descriptor.FieldType == FieldType.Fixed32
                 || descriptor.FieldType == FieldType.Enum
                )
            {

                sNumValue = value.ToString();
                if (string.IsNullOrEmpty(sNumValue))
                {
                    sNumValue = "0";
                }
            }
            else if(descriptor.FieldType == FieldType.Bool)
            {
                sBoolValue = value.ToString().ToLower();
                if(sBoolValue !="true" && sBoolValue != "1")                {
                    sBoolValue = "false";
                }
            }
            switch (descriptor.FieldType)
            {
                case FieldType.Double:                   
                    return Convert.ToDouble(sNumValue, CultureInfo.InvariantCulture);
                case FieldType.Float:                  
                    return Convert.ToSingle(sNumValue, CultureInfo.InvariantCulture);
                case FieldType.Int64:
                case FieldType.SInt64:
                case FieldType.SFixed64:
                    return Convert.ToInt64(sNumValue, CultureInfo.InvariantCulture);
                case FieldType.UInt64:
                case FieldType.Fixed64:
                    return Convert.ToUInt64(sNumValue, CultureInfo.InvariantCulture);
                case FieldType.Int32:
                case FieldType.SInt32:
                case FieldType.SFixed32:
                    return Convert.ToInt32(sNumValue, CultureInfo.InvariantCulture);
                case FieldType.Bool:
                    return Convert.ToBoolean(sBoolValue, CultureInfo.InvariantCulture);
                case FieldType.String:
                    return value;
                case FieldType.Bytes:                    {
                        if (value is string s)
                        {
                            return ByteString.FromBase64(s);
                        }
                        throw new InvalidOperationException("Base64 encoded string required to convert to bytes.");
                    }
                case FieldType.UInt32:
                case FieldType.Fixed32:
                    return Convert.ToUInt32(sNumValue, CultureInfo.InvariantCulture);
                case FieldType.Enum:                   {
                     
                        EnumValueDescriptor enumValueDescriptor;
                        if (Regex.IsMatch(sNumValue, @"\d+"))
                        {
                            var number = Convert.ToInt32(sNumValue);
                            enumValueDescriptor = descriptor.EnumType.FindValueByNumber(number);
                            if(enumValueDescriptor != null)
                            {
                                return number;
                            }
                            else
                            {
                                throw new InvalidOperationException($"Invalid enum value '{sNumValue}' for enum type {descriptor.EnumType.Name}.");
                            }                            
                        }
                        else
                        {
                            enumValueDescriptor = descriptor.EnumType.FindValueByName(sNumValue);
                            if (enumValueDescriptor == null)
                            {
                                throw new InvalidOperationException($"Invalid enum value '{sNumValue}' for enum type {descriptor.EnumType.Name}.");
                            }

                            return enumValueDescriptor.Number;
                        }
                      
                    }
                case FieldType.Message:
                    if (IsWrapperType(descriptor.MessageType))
                    {
                        return ConvertValue(value, descriptor.MessageType.FindFieldByName("value"));
                    }
                    break;
            }

            throw new InvalidOperationException("Unsupported type: " + descriptor.FieldType);
        }

        public static void RecursiveSetValue(IMessage currentValue, List<FieldDescriptor> pathDescriptors, object values)
        {
            for (var i = 0; i < pathDescriptors.Count; i++)
            {
                var isLast = i == pathDescriptors.Count - 1;
                var field = pathDescriptors[i];

                if (isLast)
                {
                    if (field.IsRepeated)
                    {
                        var list = (IList)field.Accessor.GetValue(currentValue);
                        if (values is StringValues stringValues)
                        {
                            foreach (var value in stringValues)
                            {
                                list.Add(ConvertValue(value, field));
                            }
                        }
                        else
                        {
                            list.Add(ConvertValue(values, field));
                        }
                    }
                    else
                    {
                        switch (values)
                        {
                            case StringValues {Count: 1} stringValues:
                                field.Accessor.SetValue(currentValue, ConvertValue(stringValues[0], field));
                                break;
                            case StringValues _:
                                throw new InvalidOperationException("Can't set multiple values onto a non-repeating field.");
                            case IMessage message:
                                field.Accessor.SetValue(currentValue, message);
                                break;
                            default:
                                field.Accessor.SetValue(currentValue, ConvertValue(values, field));
                                break;
                        }
                    }
                }
                else
                {
                    var fieldMessage = (IMessage)field.Accessor.GetValue(currentValue);

                    if (fieldMessage == null)
                    {
                        fieldMessage = (IMessage)Activator.CreateInstance(field.MessageType.ClrType)!;
                        field.Accessor.SetValue(currentValue, fieldMessage);
                    }

                    currentValue = fieldMessage;
                }
            }
        }

        internal static bool IsWrapperType(MessageDescriptor m) => m.File.Package == "google.protobuf" && m.File.Name == "google/protobuf/wrappers.proto";

        public static bool TryGetHttpApiOption(MethodDescriptor methodDescriptor, out HttpApiOption httpApiOption)
        {
            var options = methodDescriptor.GetOptions();
            if(options == null)
            {
                httpApiOption = null;
                return false;
            }
            
            httpApiOption = options.GetExtension(AnnotationsExtensions.HttpApiOption);
            return httpApiOption != null;
        }

        public static Dictionary<string, List<FieldDescriptor>> ResolveRouteParameterDescriptors(RoutePattern pattern, MessageDescriptor messageDescriptor)
        {
            var routeParameterDescriptors = new Dictionary<string, List<FieldDescriptor>>(StringComparer.Ordinal);
            foreach (var routeParameter in pattern.Parameters)
            {
                if (!TryResolveDescriptors(messageDescriptor, routeParameter.Name, out var fieldDescriptors))
                {
                    throw new InvalidOperationException($"Couldn't find matching field for route parameter '{routeParameter.Name}' on {messageDescriptor.Name}.");
                }
                routeParameterDescriptors.Add(routeParameter.Name, fieldDescriptors);
            }
            return routeParameterDescriptors;
        }
        /*
         public static void ResolveBodyDescriptor(string body, MethodDescriptor methodDescriptor, out MessageDescriptor? bodyDescriptor, out List<FieldDescriptor>? bodyFieldDescriptors, out bool bodyDescriptorRepeated)
         {
             bodyDescriptor = null;
             bodyFieldDescriptors = null;
             bodyDescriptorRepeated = false;
             if (!string.IsNullOrEmpty(body))
             {
                 if (!string.Equals(body, "*", StringComparison.Ordinal))
                 {
                     if (!TryResolveDescriptors(methodDescriptor.InputType, body, out bodyFieldDescriptors))
                     {
                         throw new InvalidOperationException($"Couldn't find matching field for body '{body}' on {methodDescriptor.InputType.Name}.");
                     }
                     var leafDescriptor = bodyFieldDescriptors.Last();
                     if (leafDescriptor.IsRepeated)
                     {
                         // A repeating field isn't a message type. The JSON parser will parse using the containing
                         // type to get the repeating collection.
                         bodyDescriptor = leafDescriptor.ContainingType;
                         bodyDescriptorRepeated = true;
                     }
                     else
                     {
                         bodyDescriptor = leafDescriptor.MessageType;
                     }
                 }
                 else
                 {
                     bodyDescriptor = methodDescriptor.InputType;
                 }
             }
         }
         */
    }
}