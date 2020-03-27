using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Validation;
using Hl7.FhirPath.Expressions;

namespace Fhir.Anonymizer.Core.AnonymizerConfigurations.Validation
{
    public class FhirSchemaProvider
    {
        private const string NamedBackBoneElementSuffix = "Component";
        private HashSet<string> _resourceNameSet = new HashSet<string>();
        private HashSet<string> _typeNameSet = new HashSet<string>();
        private Dictionary<string, FhirTypeNode> _fhirSchema = new Dictionary<string, FhirTypeNode>();
        public FhirSchemaProvider()
        {
            foreach (var type in GetTypesWithCustomAttribute(typeof(FhirTypeAttribute)))
            {
                var typeAttribute = type.GetCustomAttribute<FhirTypeAttribute>();
                var typeNameKey = GetTypeNameKey(type);

                if (_fhirSchema.ContainsKey(typeNameKey))
                {
                    continue;
                }

                if (typeAttribute.IsResource)
                {
                    _resourceNameSet.Add(typeNameKey);
                }
                else
                {
                    _typeNameSet.Add(typeNameKey);
                }

                var typeNode = new FhirTypeNode
                {
                    InstanceType = typeNameKey,
                    Name = string.Empty,
                    IsResource = typeAttribute.IsResource,
                };
                var childrens = new Dictionary<string, IEnumerable<FhirTypeNode>>();

                if (!IsPrimitiveType(type))
                {
                    var properties = type.GetProperties();
                    foreach (var property in properties)
                    {
                        var elementAttribute = property.GetCustomAttributes<FhirElementAttribute>().FirstOrDefault();
                        if (elementAttribute != null)
                        {
                            var fieldTypes = new List<Type>();
                            if (elementAttribute.Choice != ChoiceType.None)
                            {
                                var allowedTypeAttribute = property.GetCustomAttributes<AllowedTypesAttribute>().FirstOrDefault();
                                fieldTypes.AddRange(allowedTypeAttribute.Types);
                            }
                            else
                            {
                                var propertyType = property.PropertyType;
                                // Get actual Type from generic field
                                if (propertyType.IsGenericType)
                                {
                                    propertyType = property.PropertyType.GetGenericArguments().First();
                                }
                                fieldTypes.Add(propertyType);
                            }

                            var nodes = fieldTypes.Select(type =>
                                new FhirTypeNode
                                {
                                    InstanceType = GetTypeNameKey(type),
                                    Name = elementAttribute.Name,
                                    IsResource = elementAttribute.Choice == ChoiceType.ResourceChoice,
                                    Parent = typeNode
                                });
                            childrens.Add(elementAttribute.Name, nodes);
                        }
                    }
                }
                typeNode.Children = childrens;
                _fhirSchema.Add(typeNameKey, typeNode);
            }
        }

        public RuleValidationResult ValidateRule(string path, string method, AnonymizerRuleType type, HashSet<string> methodSupportedFieldTypes)
        {
            var pathComponents = path.Split('.', StringSplitOptions.None);
            if (!pathComponents.Any() || pathComponents.Where(string.IsNullOrEmpty).Any())
            {
                return new RuleValidationResult
                {
                    Success = false,
                    ErrorMessage = $"{path} is invalid."
                };
            }

            var currentTypeName = pathComponents.First();

            if (type == AnonymizerRuleType.TypeRule) // Type rules start with data type
            {
                if (!_typeNameSet.Contains(currentTypeName))
                {
                    return new RuleValidationResult
                    {
                        Success = false,
                        ErrorMessage = $"{currentTypeName} is an invalid data type."
                    };
                }
                else if (currentTypeName.Equals("BackboneElement"))
                {
                    return new RuleValidationResult
                    {
                        Success = false,
                        ErrorMessage = $"{currentTypeName} is a valid but not supported data type."
                    };
                }
            }
            // Path rules start with resource type
            else
            {
                if (!_resourceNameSet.Contains(currentTypeName))
                {
                    return new RuleValidationResult
                    {
                        Success = false,
                        ErrorMessage = $"{currentTypeName} is an invalid resource type."
                    };
                }
                else if (path.StartsWith("Bundle.entry") || path.StartsWith($"{currentTypeName}.contained"))
                {
                    return new RuleValidationResult
                    {
                        Success = false,
                        ErrorMessage = $"Path of Bundle/contained resources is not supported."
                    };
                }
            }

            var pathValidationResult = ValidateRulePathComponents(pathComponents.ToList(), 1, currentTypeName);
            if (!pathValidationResult.Success)
            {
                return pathValidationResult;
            }

            if (!Enum.TryParse<AnonymizerMethod>(method, true, out _))
            {
                return new RuleValidationResult
                {
                    Success = false,
                    ErrorMessage = $"Anonymization method {method} is currently not supported."
                };
            }

            if (methodSupportedFieldTypes != null && !methodSupportedFieldTypes.Contains(pathValidationResult.TargetDataType)) 
            {
                return new RuleValidationResult
                {
                    Success = false,
                    ErrorMessage = $"Anonymization method {method} cannot be applied to {string.Join('.', pathComponents)}."
                };
            }

            return new RuleValidationResult
            {
                Success = true,
                TargetDataType = pathValidationResult.TargetDataType
            };
        }

        public HashSet<string> GetFhirResourceTypes()
        {
            return _resourceNameSet;
        }

        public HashSet<string> GetFhirDataTypes()
        {
            return _typeNameSet;
        }

        public HashSet<string> GetFhirAllTypes()
        {
            return _resourceNameSet.Union(_typeNameSet).ToHashSet();
        }

        public Dictionary<string, FhirTypeNode> GetFhirSchema()
        {
            return _fhirSchema;
        }

        private RuleValidationResult ValidateRulePathComponents(List<string> pathComponents, int index, string typeName)
        {
            if (index >= pathComponents.Count())
            {
                return new RuleValidationResult
                {
                    Success = true,
                    TargetDataType = typeName
                };
            }

            var typeSchema = _fhirSchema.GetValueOrDefault(typeName);
            if (typeSchema == null)
            {
                return new RuleValidationResult
                {
                    Success = false,
                    ErrorMessage = $"{typeName} is an invalid data type."
                };
            }

            var fieldName = pathComponents[index];
            if (!typeSchema.Children.ContainsKey(fieldName))
            {
                return new RuleValidationResult
                {
                    Success = false,
                    ErrorMessage = $"{fieldName} is an invalid field in {string.Join('.', pathComponents.Take(index))}."
                };

            }

            string errorMessage = string.Empty;
            foreach(var node in typeSchema.Children[fieldName])
            {
                var result = ValidateRulePathComponents(pathComponents, index + 1, node.InstanceType);
                if (result.Success)
                {
                    return result;
                }

                errorMessage = result.ErrorMessage;
            }

            return new RuleValidationResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        private bool IsPrimitiveType(Type type)
        {
            // either this type is subclass of Primitive or in System namespace
            if (typeof(Primitive).IsAssignableFrom(type) || IsEnumType(type))
            {
                return true;
            }
            return false;
        }

        private bool IsEnumType(Type type) 
        {
            Type currentType = type;
            while (currentType != null)
            {
                if (currentType.IsEnum)
                {
                    return true;
                }

                currentType = currentType.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Resolve all types from assembly with a attribute
        /// </summary>
        /// <param name="attributeType"></param>
        /// <returns></returns>
        private IEnumerable<Type> GetTypesWithCustomAttribute(Type attributeType)
        {
            var assembly = attributeType.Assembly;
            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(attributeType, false).Length > 0)
                {
                    yield return type;
                }
            }
        }

        /// <summary>
        /// Get key for a FhirType object, return an alias of "ResourceName_FieldName" for a BackboneElement type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private string GetTypeNameKey(Type type)
        {
            var typeAttribute = type.GetCustomAttribute<FhirTypeAttribute>();
            // Enum data type members => string
            if (typeAttribute == null && IsPrimitiveType(type))
            {
                return "code";
            }

            if (!typeAttribute.NamedBackboneElement)
            {
                return typeAttribute.Name;
            }
            else
            {
                var resourceType = type.DeclaringType;
                if (resourceType != null)
                {
                    var resourceAttribute = resourceType.GetCustomAttribute<FhirTypeAttribute>();
                    // Resolve fieldName for NamedBackboneElement Type, i.e. ItemComponent => "item"
                    if (typeAttribute.Name.Length > NamedBackBoneElementSuffix.Length)
                    {
                        var fieldName = typeAttribute.Name.Substring(0, typeAttribute.Name.Length - NamedBackBoneElementSuffix.Length).ToLower();
                        return $"{resourceAttribute.Name}*{fieldName}";
                    }
                }
                return typeAttribute.Name;
            }
        }

    }
}
