﻿using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Cauldron.Interception.Fody
{
    internal static class Extensions
    {
        private static ModuleWeaver _moduleWeaver;
        private static IEnumerable<AssemblyDefinition> allAssemblies;
        private static IEnumerable<TypeDefinition> allTypes;
        private static ModuleDefinition moduleDefinition;

        public static ModuleWeaver ModuleWeaver
        {
            set
            {
                _moduleWeaver = value;
                moduleDefinition = value.ModuleDefinition;
                allAssemblies = value.ModuleDefinition.AssemblyReferences.GetAllAssemblyDefinitions()
                    .Concat(new AssemblyDefinition[] { value.ModuleDefinition.Assembly }).ToArray();

                foreach (var item in allAssemblies)
                    _moduleWeaver.LogInfo("<<Assembly>> " + item.FullName);

                allTypes = allAssemblies.SelectMany(x => x.Modules).Where(x => x != null).SelectMany(x => x.Types).Where(x => x != null).Concat(value.ModuleDefinition.Types).ToArray();
            }
        }

        public static void AddDebuggerBrowsableAttribute(this Collection<CustomAttribute> customAttributeCollection, DebuggerBrowsableState state)
        {
            var ctor = typeof(DebuggerBrowsableAttribute).GetMethodReference(".ctor", new Type[] { typeof(DebuggerBrowsableState) }).Import();
            var attribute = new CustomAttribute(ctor);

            attribute.ConstructorArguments.Add(new CustomAttributeArgument(typeof(DebuggerBrowsableState).GetTypeReference().Import(), state));
            customAttributeCollection.Add(attribute);
        }

        public static void AddEditorBrowsableAttribute(this Collection<CustomAttribute> customAttributeCollection, EditorBrowsableState state)
        {
            var ctor = typeof(EditorBrowsableAttribute).GetMethodReference(".ctor", new Type[] { typeof(EditorBrowsableState) }).Import();
            var attribute = new CustomAttribute(ctor);

            attribute.ConstructorArguments.Add(new CustomAttributeArgument(typeof(EditorBrowsableState).GetTypeReference().Import(), state));
            customAttributeCollection.Add(attribute);
        }

        public static IEnumerable<AssemblyDefinition> AllReferencedAssemblies(this ModuleDefinition target) => allAssemblies;

        public static void Append(this ILProcessor processor, IEnumerable<Instruction> instructions)
        {
            foreach (var instruction in instructions)
                processor.Append(instruction);
        }

        public static IEnumerable<Instruction> Call(this ILProcessor processor, object instance, MethodReference methodReference, params object[] parameters) =>
            SetupParameter(processor, OpCodes.Call, instance, methodReference, parameters);

        public static IEnumerable<Instruction> Callvirt(this ILProcessor processor, object instance, MethodReference methodReference, params object[] parameters) =>
            SetupParameter(processor, OpCodes.Callvirt, instance, methodReference, parameters);

        /// <summary>
        /// Gets the number of elements contained in the <see cref="IEnumerable"/>
        /// </summary>
        /// <param name="source">The <see cref="IEnumerable"/></param>
        /// <returns>The total count of items in the <see cref="IEnumerable"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null</exception>
        public static int Count_(this IEnumerable source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            int count = 0;

            if (source.GetType().IsArray)
                return (source as Array).Length;

            var collection = source as ICollection;
            if (collection != null)
                return collection.Count;

            var enumerator = source.GetEnumerator();
            while (enumerator.MoveNext())
                count++;

            (enumerator as IDisposable)?.Dispose();

            return count;
        }

        public static FieldReference CreateFieldReference(this FieldDefinition field)
        {
            if (field.DeclaringType.HasGenericParameters)
            {
                var declaringType = new GenericInstanceType(field.DeclaringType);

                foreach (var parameter in field.DeclaringType.GenericParameters)
                    declaringType.GenericArguments.Add(parameter);

                return new FieldReference(field.Name, field.FieldType, declaringType);
            }

            return field;
        }

        public static FieldReference CreateFieldReference(this FieldReference field)
        {
            if (field.DeclaringType.HasGenericParameters)
            {
                var declaringType = new GenericInstanceType(field.DeclaringType);

                foreach (var parameter in field.DeclaringType.GenericParameters)
                    declaringType.GenericArguments.Add(parameter);

                return new FieldReference(field.Name, field.FieldType, declaringType);
            }

            return field;
        }

        public static MethodReference CreateMethodReference(this MethodDefinition method)
        {
            if (method.DeclaringType.HasGenericParameters)
            {
                var declaringType = new GenericInstanceType(method.DeclaringType);
                return method.MakeHostInstanceGeneric(method.DeclaringType.GenericParameters.ToArray());
            }

            return method;
        }

        public static IEnumerable<AssemblyDefinition> GetAllAssemblyDefinitions(this IEnumerable<AssemblyNameReference> target)
        {
            var result = new List<AssemblyDefinition>();
            result.AddRange(target.Select(x => moduleDefinition.AssemblyResolver.Resolve(x)).Where(x => x != null));

            foreach (var item in target)
            {
                var assembly = moduleDefinition.AssemblyResolver.Resolve(item);

                if (assembly == null)
                    continue;

                result.Add(assembly);

                if (assembly.MainModule.HasAssemblyReferences)
                {
                    foreach (var a in assembly.Modules)
                        GetAllAssemblyDefinitions(a.AssemblyReferences, result);
                }
            }

            return result.Distinct(new AssemblyDefinitionEqualityComparer()).OrderBy(x => x.FullName);
        }

        public static IEnumerable<TypeReference> GetBaseClassAndInterfaces(this TypeDefinition type)
        {
            var result = new List<TypeReference>();
            result.Add(type);

            if (type.Interfaces != null && type.Interfaces.Count > 0)
                result.AddRange(type.Interfaces.Select(x => x.Resolve()));

            if (type.BaseType != null)
                result.AddRange(type.BaseType.Resolve().GetBaseClassAndInterfaces());

            return result;
        }

        public static TypeReference GetChildrenType(this TypeReference type)
        {
            if (type.IsArray)
                return type.GetElementType();

            var getIEnumerableInterfaceChild = new Func<TypeReference, TypeReference>(typeReference =>
            {
                if (typeReference.IsGenericInstance)
                {
                    var genericType = typeReference as GenericInstanceType;

                    if (genericType != null)
                    {
                        var genericInstances = genericType.GetGenericInstances();

                        // We have to make some exceptions to dictionaries
                        var ienumerableInterface = genericInstances.FirstOrDefault(x => x.FullName.StartsWith("System.Collections.Generic.IDictionary`2<"));

                        // If we have more than 1 generic argument then we try to get a IEnumerable<> interface
                        // otherwise we just return the last argument in the list
                        if (ienumerableInterface == null)
                            ienumerableInterface = genericInstances.FirstOrDefault(x => x.FullName.StartsWith("System.Collections.Generic.IEnumerable`1<"));

                        // We just don't know :(
                        if (ienumerableInterface == null)
                            return typeof(object).GetTypeReference();

                        return (ienumerableInterface as GenericInstanceType).GenericArguments[0];
                    }
                }

                return null;
            });

            var result = getIEnumerableInterfaceChild(type);

            if (result != null)
                return result;

            // This might be a type that inherits from list<> or something...
            // lets find out
            if (type.Resolve().GetBaseClassAndInterfaces().Any(x => x.FullName == "System.Collections.Generic.IEnumerable`1"))
            {
                // if this is the case we will dig until we find a generic instance type
                var baseType = type.Resolve().BaseType;

                while (baseType != null)
                {
                    result = getIEnumerableInterfaceChild(baseType);

                    if (result != null)
                        return result;

                    baseType = baseType.Resolve().BaseType;
                };
            }

            return typeof(object).GetTypeReference();
        }

        public static MethodReference GetDefaultConstructor(this TypeReference type)
        {
            if (!type.HasMethod(".ctor", 0))
                return null;

            return type.GetMethodReference(".ctor", 0);
        }

        public static IEnumerable<TypeReference> GetGenericInstances(this GenericInstanceType type)
        {
            var result = new List<TypeReference>();
            result.Add(type);

            var resolved = type.Resolve();
            var genericArgumentsNames = resolved.GenericParameters.Select(x => x.FullName).ToArray();
            var genericArguments = type.GenericArguments.ToArray();

            if (resolved.BaseType != null)
                result.AddRange(resolved.BaseType.GetGenericInstances(genericArgumentsNames, genericArguments));

            if (resolved.Interfaces != null && resolved.Interfaces.Count > 0)
            {
                foreach (var item in resolved.Interfaces)
                    result.AddRange(item.GetGenericInstances(genericArgumentsNames, genericArguments));
            }

            return result;
        }

        public static IEnumerable<TypeReference> GetInterfaces(this TypeDefinition type)
        {
            var result = new List<TypeReference>();

            if (type.Interfaces != null && type.Interfaces.Count > 0)
                result.AddRange(type.Interfaces.Select(x => x.Resolve()).Where(x => x != null));

            if (type.BaseType != null)
                result.AddRange(type.BaseType.Resolve().GetInterfaces().Where(x => x != null));

            return result;
        }

        public static IEnumerable<MethodReference> GetMethodDefinitionsByName(this TypeReference type, string methodName) =>
            type.GetMethodReferences().Where(x => x.Name == methodName).ToArray();

        public static MethodReference GetMethodReference(this Type type, string methodName, Type[] parameterTypes)
        {
            var definition = type.GetTypeDefinition();
            var result = definition.GetMethodDefinitionsByName(methodName).FirstOrDefault(x => x.Name == methodName && parameterTypes.Select(y => y.FullName).SequenceEqual(x.Parameters.Select(y => y.ParameterType.FullName)));

            if (result != null)
                return result;

            throw new Exception($"Unable to proceed. The type '{type.FullName}' does not contain a method '{methodName}'");
        }

        public static MethodReference GetMethodReference(this TypeReference typeReference, string methodName, int parameterCount)
        {
            var definition = typeReference.Resolve();
            var result = typeReference.GetMethodDefinitionsByName(methodName).FirstOrDefault(x => x.Name == methodName && x.Parameters.Count == parameterCount);

            if (result != null)
                return result;

            throw new Exception($"Unable to proceed. The type '{typeReference.FullName}' does not contain a method '{methodName}'");
        }

        public static MethodReference GetMethodReference(this Type type, string methodName, int parameterCount)
        {
            var definition = type.GetTypeDefinition();
            var result = definition.GetMethodDefinitionsByName(methodName).FirstOrDefault(x => x.Name == methodName && x.Parameters.Count == parameterCount);

            if (result != null)
                return result;

            throw new Exception($"Unable to proceed. The type '{type.FullName}' does not contain a method '{methodName}'");
        }

        public static IEnumerable<MethodReference> GetMethodReferences(this TypeReference type)
        {
            var result = new List<MethodReference>();
            var baseType = type;

            while (baseType != null)
            {
                if (baseType.IsGenericInstance)
                {
                    var genericType = baseType as GenericInstanceType;

                    if (genericType != null)
                    {
                        var instances = genericType.GetGenericInstances().Where(x => !x.Resolve().IsInterface);

                        foreach (var item in instances)
                        {
                            var methods = item.Resolve().Methods.Select(x => x.MakeHostInstanceGeneric((item as GenericInstanceType).GenericArguments.ToArray()));

                            if (methods != null || methods.Any())
                                result.AddRange(methods);
                        }
                    }
                }
                else
                {
                    var methods = baseType.Resolve().Methods;

                    if (methods != null || methods.Any())
                        result.AddRange(methods);
                }

                baseType = baseType.Resolve().BaseType;
            };

            return result.Distinct(new MethodReferenceEqualityComparer());
        }

        public static IEnumerable<MethodReference> GetMethodReferences(this TypeReference typeReference, string methodName, int parameterCount)
        {
            var definition = typeReference.Resolve();
            return typeReference.GetMethodDefinitionsByName(methodName).Where(x => x.Name == methodName && x.Parameters.Count == parameterCount).ToArray();
        }

        public static IEnumerable<MethodDefinition> GetMethods(this TypeDefinition type) => type.Methods.Concat(type.GetInterfaces().SelectMany(x => x.Resolve().Methods));

        public static IEnumerable<TypeDefinition> GetNestedTypes(this TypeDefinition type)
        {
            var result = new List<TypeDefinition>();

            if (type.HasNestedTypes)
                foreach (var item in type.NestedTypes)
                {
                    result.Add(item);

                    if (type.HasNestedTypes)
                        result.AddRange(item.GetNestedTypes());
                }

            return result;
        }

        public static IEnumerable<PropertyDefinition> GetProperties(this TypeDefinition type) => type.Properties.Concat(type.GetInterfaces().SelectMany(x => x.Resolve().Properties));

        public static PropertyDefinition GetPropertyDefinition(this MethodDefinition method) =>
            method.DeclaringType.Properties.FirstOrDefault(x => x.GetMethod == method || x.SetMethod == method);

        /// <summary>
        /// Gets the stacktrace of the exception and the inner exceptions recursively
        /// </summary>
        /// <param name="e">The exception with the stack trace</param>
        /// <returns>A string representation of the stacktrace</returns>
        public static string GetStackTrace(this Exception e)
        {
            var sb = new StringBuilder();
            var ex = e;

            do
            {
                sb.AppendLine("Exception Type: " + ex.GetType().Name);
                sb.AppendLine("Source: " + ex.Source);
                sb.AppendLine(ex.Message);
                sb.AppendLine("------------------------");
                sb.AppendLine(ex.StackTrace);
                sb.AppendLine("------------------------");

                ex = ex.InnerException;
            } while (ex != null);

            return sb.ToString();
        }

        public static TypeDefinition GetTypeDefinition(this Type type)
        {
            var result = allTypes.FirstOrDefault(x => x.FullName == type.FullName);

            if (result == null)
                throw new Exception($"Unable to proceed. The type '{type.FullName}' was not found.");

            return result;
        }

        public static TypeReference GetTypeReference(this Type type)
        {
            var result = allTypes.FirstOrDefault(x => x.FullName == type.FullName);

            if (result == null)
                throw new Exception($"Unable to proceed. The type '{type.FullName}' was not found.");

            return result;
        }

        public static IEnumerable<TypeDefinition> GetTypesThatImplementsInterface(this TypeDefinition typeDefinitionOfInterface) =>
            allTypes.Where(x => x.ImplementsInterface(typeDefinitionOfInterface.FullName)).ToArray();

        public static bool HasMethod(this TypeReference typeReference, string methodName) => typeReference.HasMethod(methodName, 0);

        public static bool HasMethod(this TypeReference typeReference, string methodName, int parameterCount) =>
            typeReference.GetMethodReferences(methodName, parameterCount).Any();

        public static IEnumerable<Instruction> If(this ILProcessor processor, Action<List<Instruction>> action, Instruction exit)
        {
            var instructions = new List<Instruction>();
            action(instructions);

            var endif = processor.Create(OpCodes.Nop);
            instructions.Add(processor.Create(OpCodes.Ldc_I4_1));
            instructions.Add(processor.Create(OpCodes.Ceq));
            instructions.Add(processor.Create(OpCodes.Brfalse_S, endif));
            instructions.Add(processor.Create(OpCodes.Leave_S, exit));
            instructions.Add(endif);

            return instructions;
        }

        public static bool ImplementsInterface(this TypeDefinition type, Type interfaceType) =>
            type.GetInterfaces().Any(x => x.FullName == interfaceType.FullName);

        public static bool ImplementsInterface(this TypeDefinition type, string interfaceName) =>
            type.GetInterfaces().Any(x => x.FullName == interfaceName);

        public static TypeReference Import(this TypeReference value) => moduleDefinition.Import(value);

        public static MethodReference Import(this System.Reflection.MethodBase value) => moduleDefinition.Import(value);

        public static TypeReference Import(this Type value) => moduleDefinition.Import(value);

        public static MethodReference Import(this MethodReference value) => moduleDefinition.Import(value);

        public static FieldReference Import(this FieldReference value) => moduleDefinition.Import(value);

        public static void InsertAfter(this ILProcessor processor, Instruction target, IEnumerable<Instruction> instructions)
        {
            var last = target;

            foreach (var instruction in instructions)
            {
                processor.InsertAfter(last, instruction);
                last = instruction;
            }
        }

        public static void InsertBefore(this ILProcessor processor, Instruction target, IEnumerable<Instruction> instructions)
        {
            foreach (var instruction in instructions)
                processor.InsertBefore(target, instruction);
        }

        public static void InsertToCtorOrCctor(this TypeReference typeReference, bool isStatic, Func<ILProcessor, List<Instruction>, InsertionPosition> action)
        {
            var list = new List<Instruction>();
            var insertAction = new Action<MethodReference>(ctor =>
            {
                var body = ctor.Resolve().Body;
                var ctorProcessor = body.GetILProcessor();
                var first = body.Instructions.FirstOrDefault(x => x.OpCode == OpCodes.Call && (x.Operand as MethodReference).Name == ".ctor") ?? body.Instructions.FirstOrDefault();

                if (action(ctorProcessor, list) == InsertionPosition.InsertAfterBaseCall)
                    ctorProcessor.InsertAfter(first, list);
                else
                    ctorProcessor.InsertBefore(first, list);
            });

            if (isStatic)
                insertAction(GetOrCreateStaticConstructor(typeReference));
            else
            {
                foreach (var constructor in GetConstructors(typeReference))
                    insertAction(constructor);
            }
        }

        public static bool IsAttribute(this TypeDefinition typeDefinition)
        {
            typeDefinition = typeDefinition.BaseType?.Resolve();

            while (typeDefinition != null)
            {
                if (typeDefinition.FullName == "System.Attribute")
                    return true;

                typeDefinition = typeDefinition.BaseType?.Resolve();
            }

            return false;
        }

        public static bool IsIEnumerable(this TypeReference type)
        {
            var resolved = type.Resolve();

            if (resolved == null)
                return false;

            return
                type.FullName != typeof(string).FullName /* Strings are arrays too */ &&
                (
                    resolved.ImplementsInterface(typeof(IList)) ||
                    resolved.ImplementsInterface(typeof(IEnumerable)) ||
                    type.IsArray ||
                    type.FullName.EndsWith("[]") ||
                    resolved.IsArray ||
                    resolved.FullName.EndsWith("[]")
                );
        }

        public static MethodReference MakeGeneric(this MethodReference method, params TypeReference[] args)
        {
            if (args.Length == 0)
                return method;

            if (method.GenericParameters.Count != args.Length)
                throw new ArgumentException("Invalid number of generic type arguments supplied");

            var genericTypeRef = new GenericInstanceMethod(method);

            foreach (var arg in args)
                genericTypeRef.GenericArguments.Add(arg);

            return genericTypeRef;
        }

        public static MethodReference MakeHostInstanceGeneric(this MethodReference self, params TypeReference[] arguments)
        {
            // https://groups.google.com/forum/#!topic/mono-cecil/mCat5UuR47I by ShdNx

            var reference = new MethodReference(self.Name, self.ReturnType, self.DeclaringType.MakeGenericInstanceType(arguments))
            {
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention
            };

            foreach (var parameter in self.Parameters)
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

            foreach (var generic_parameter in self.GenericParameters)
                reference.GenericParameters.Add(new GenericParameter(generic_parameter.Name, reference));

            return reference;
        }

        public static IEnumerable<Instruction> MethodOf(this ILProcessor processor, MethodDefinition method)
        {
            var methodRef = method.CreateMethodReference();
            return new Instruction[] {
                processor.Create(OpCodes.Ldtoken, methodRef),
                processor.Create(OpCodes.Ldtoken, methodRef.DeclaringType),
                processor.Create(OpCodes.Call, typeof(System.Reflection.MethodBase).GetMethodReference("GetMethodFromHandle", 2).Import())
            };
        }

        public static IEnumerable<Instruction> Newobj(this ILProcessor processor, FieldDefinition field, MethodReference methodReference, params object[] parameters)
        {
            var instructions = new List<Instruction>();
            if (!field.IsStatic)
                instructions.Add(processor.Create(OpCodes.Ldarg_0));

            var fieldRef = field.CreateFieldReference();

            instructions.AddRange(SetupParameter(processor, OpCodes.Newobj, null, methodReference, parameters));
            instructions.Add(processor.Create(field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, fieldRef));
            return instructions;
        }

        public static IEnumerable<Instruction> Newobj(this ILProcessor processor, VariableDefinition local, MethodReference methodReference, params object[] parameters)
        {
            var instructions = new List<Instruction>();
            instructions.AddRange(SetupParameter(processor, OpCodes.Newobj, null, methodReference, parameters));
            instructions.Add(processor.Create(OpCodes.Ldloca_S, local));
            return instructions;
        }

        public static IEnumerable<Instruction> Newobj(this ILProcessor processor, MethodReference methodReference, params object[] parameters) =>
            SetupParameter(processor, OpCodes.Newobj, null, methodReference, parameters);

        /// <summary>
        /// Converts a <see cref="IEnumerable"/> to an array
        /// </summary>
        /// <typeparam name="T">The type of elements the <see cref="IEnumerable"/> contains</typeparam>
        /// <param name="items">The <see cref="IEnumerable"/> to convert</param>
        /// <returns>An array of <typeparamref name="T"/></returns>
        public static T[] ToArray_<T>(this IEnumerable items)
        {
            if (items == null)
                return new T[0];

            T[] result = new T[items.Count_()];
            int counter = 0;

            foreach (T item in items)
            {
                result[counter] = item;
                counter++;
            }

            return result;
        }

        public static TypeDefinition ToTypeDefinition(this string typeName) => allTypes.FirstOrDefault(x => x.FullName == typeName || x.FullName.EndsWith(typeName) || x.Name == typeName);

        public static IEnumerable<Instruction> TypeOf(this ILProcessor processor, TypeReference type)
        {
            return new Instruction[] {
                processor.Create(OpCodes.Ldtoken, type),
                processor.Create(OpCodes.Call, typeof(Type).GetMethodReference("GetTypeFromHandle", 1).Import())
            };
        }

        private static VariableDefinition AddVariableDefinitionToInstruction(ILProcessor processor, List<Instruction> instructions, MethodDefinition methodDef, object p)
        {
            var value = p as VariableDefinition;
            var variableDef = methodDef.Body.Variables.FirstOrDefault(x => x == p);
            var index = int.MaxValue;

            if (variableDef == null)
                index = methodDef.Body.Variables.IndexOf(variableDef);

            switch (index)
            {
                case 0: instructions.Add(processor.Create(OpCodes.Ldloc_0)); break;
                case 1: instructions.Add(processor.Create(OpCodes.Ldloc_1)); break;
                case 2: instructions.Add(processor.Create(OpCodes.Ldloc_2)); break;
                case 3: instructions.Add(processor.Create(OpCodes.Ldloc_3)); break;
                default:
                    instructions.Add(processor.Create(OpCodes.Ldloc_S, value));
                    break;
            }

            return value;
        }

        private static void GetAllAssemblyDefinitions(IEnumerable<AssemblyNameReference> target, List<AssemblyDefinition> result)
        {
            result.AddRange(target.Select(x => moduleDefinition.AssemblyResolver.Resolve(x)).Where(x => x != null));

            foreach (var item in target)
            {
                var assembly = moduleDefinition.AssemblyResolver.Resolve(item);

                if (assembly == null)
                    continue;

                if (result.Contains(assembly))
                    continue;

                result.Add(assembly);

                if (assembly.MainModule.HasAssemblyReferences)
                {
                    foreach (var a in assembly.Modules)
                        GetAllAssemblyDefinitions(a.AssemblyReferences, result);
                }
            }
        }

        private static IEnumerable<MethodReference> GetConstructors(TypeReference type)
        {
            var resolvedType = type.Resolve();

            if (resolvedType == null)
                return new MethodReference[0];

            var result = new List<MethodReference>();

            foreach (var constructor in resolvedType.Methods.Where(x => x.Name == ".ctor"))
            {
                foreach (var instruction in constructor.Body.Instructions.Where(x => x.OpCode == OpCodes.Call))
                {
                    var methodReference = instruction.Operand as MethodReference;

                    if (methodReference == null)
                        continue;

                    if (resolvedType.BaseType.FullName != methodReference.DeclaringType.FullName && !methodReference.FullName.Contains(".ctor"))
                        continue;

                    if (methodReference.DeclaringType.FullName != type.FullName)
                    {
                        result.Add(constructor);
                        break;
                    }
                }
            }

            return result;
        }

        private static IEnumerable<TypeReference> GetGenericInstances(this TypeReference type, string[] genericArgumentNames, TypeReference[] genericArgumentsOfCurrentType)
        {
            var resolvedBase = type.Resolve();

            if (resolvedBase.HasGenericParameters)
            {
                var genericArguments = new List<TypeReference>();

                foreach (var arg in (type as GenericInstanceType).GenericArguments)
                {
                    var t = genericArgumentNames.FirstOrDefault(x => x == arg.FullName);

                    if (t == null)
                        genericArguments.Add(arg);
                    else
                        genericArguments.Add(genericArgumentsOfCurrentType[Array.IndexOf(genericArgumentNames, t)]);
                }

                var genericType = resolvedBase.MakeGenericInstanceType(genericArguments.ToArray());
                return genericType.GetGenericInstances();
            }

            return new TypeReference[0];
        }

        private static MethodDefinition GetOrCreateStaticConstructor(TypeReference type)
        {
            var resolvedType = type.Resolve();

            if (resolvedType == null)
                return null;

            var cctor = resolvedType.GetStaticConstructor();

            if (cctor != null)
                return cctor;

            var method = new MethodDefinition(".cctor", MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, _moduleWeaver.ModuleDefinition.TypeSystem.Void);

            var processor = method.Body.GetILProcessor();
            processor.Append(processor.Create(OpCodes.Ret));

            resolvedType.Methods.Add(method);

            return method;
        }

        private static IEnumerable<Instruction> SetupParameter(ILProcessor processor, OpCode opCode, object instance, MethodReference methodReference, params object[] parameters)
        {
            // Extend this method if neccessary... Not all scenarios are handled here
            var instructions = new List<Instruction>();
            var methodDef = processor.Body.Method;
            var isStatic = methodDef.IsStatic;

            if (instance != null && instance.GetType() == typeof(VariableDefinition))
                AddVariableDefinitionToInstruction(processor, instructions, methodDef, instance);

            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];

                if (p == null)
                {
                    instructions.Add(processor.Create(OpCodes.Ldnull));
                    continue;
                }

                var type = p.GetType();
                var targetType = methodReference.Parameters[i].ParameterType;
                TypeReference paramType = null;

                if (type == typeof(string))
                {
                    instructions.Add(processor.Create(OpCodes.Ldstr, p.ToString()));
                    paramType = typeof(string).GetTypeReference();
                }
                else if (type == typeof(FieldDefinition))
                {
                    var value = p as FieldDefinition;

                    if (!isStatic)
                        instructions.Add(processor.Create(OpCodes.Ldarg_0));

                    instructions.Add(processor.Create(isStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, value.CreateFieldReference()));
                    paramType = value.FieldType;
                }
                else if (type == typeof(FieldReference))
                {
                    var value = p as FieldReference;

                    if (!isStatic)
                        instructions.Add(processor.Create(OpCodes.Ldarg_0));

                    instructions.Add(processor.Create(isStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, value));
                    paramType = value.FieldType;
                }
                else if (type == typeof(int))
                {
                    instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)p));
                    paramType = typeof(int).GetTypeReference();
                }
                else if (type == typeof(uint))
                {
                    instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)(uint)p));
                    paramType = typeof(uint).GetTypeReference();
                }
                else if (type == typeof(bool))
                {
                    instructions.Add(processor.Create(OpCodes.Ldc_I4, (bool)p ? 1 : 0));
                    paramType = typeof(bool).GetTypeReference();
                }
                else if (type == typeof(char))
                {
                    instructions.Add(processor.Create(OpCodes.Ldc_I4, (char)p));
                    paramType = typeof(char).GetTypeReference();
                }
                else if (type == typeof(short))
                {
                    instructions.Add(processor.Create(OpCodes.Ldc_I4, (short)p));
                    paramType = typeof(short).GetTypeReference();
                }
                else if (type == typeof(ushort))
                {
                    instructions.Add(processor.Create(OpCodes.Ldc_I4, (ushort)p));
                    paramType = typeof(ushort).GetTypeReference();
                }
                else if (type == typeof(byte))
                {
                    instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)(byte)p));
                    paramType = typeof(byte).GetTypeReference();
                }
                else if (type == typeof(sbyte))
                {
                    instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)(sbyte)p));
                    paramType = typeof(sbyte).GetTypeReference();
                }
                else if (type == typeof(long))
                {
                    instructions.Add(processor.Create(OpCodes.Ldc_I8, (long)p));
                    paramType = typeof(long).GetTypeReference();
                }
                else if (type == typeof(ulong))
                {
                    instructions.Add(processor.Create(OpCodes.Ldc_I8, (long)(ulong)p));
                    paramType = typeof(ulong).GetTypeReference();
                }
                else if (type == typeof(double))
                {
                    instructions.Add(processor.Create(OpCodes.Ldc_R8, (double)p));
                    paramType = typeof(double).GetTypeReference();
                }
                else if (type == typeof(float))
                {
                    instructions.Add(processor.Create(OpCodes.Ldc_R4, (float)p));
                    paramType = typeof(float).GetTypeReference();
                }
                else if (type == typeof(VariableDefinition))
                {
                    var value = AddVariableDefinitionToInstruction(processor, instructions, methodDef, p);
                    paramType = value.VariableType;
                }
                else if (type == typeof(This))
                {
                    if (!isStatic)
                        instructions.Add(processor.Create(OpCodes.Ldarg_0));
                    else
                        instructions.Add(processor.Create(OpCodes.Ldnull));

                    paramType = methodDef.DeclaringType;
                }
                else if (type == typeof(ParameterDefinition))
                {
                    var value = p as ParameterDefinition;
                    instructions.Add(processor.Create(OpCodes.Ldarg_S, value));
                    paramType = value.ParameterType;
                }
                else if (p is IEnumerable<Instruction>)
                {
                    foreach (var item in p as IEnumerable<Instruction>)
                        instructions.Add(item);
                }

                if (paramType != null && paramType.Resolve() == null)
                    instructions.Add(processor.Create(OpCodes.Box, paramType));

                if (paramType != null && paramType.IsValueType && !targetType.IsValueType)
                    instructions.Add(processor.Create(OpCodes.Box, paramType));
            }

            instructions.Add(processor.Create(opCode, methodReference));
            return instructions;
        }
    }
}