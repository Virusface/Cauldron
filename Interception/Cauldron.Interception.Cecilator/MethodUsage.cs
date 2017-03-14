﻿using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Cauldron.Interception.Cecilator
{
    public class MethodUsage : CecilatorBase, IEquatable<MethodUsage>
    {
        [EditorBrowsable(EditorBrowsableState.Never), DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Instruction instruction;

        internal MethodUsage(Method method, MethodDefinition methodDef, Instruction instruction) : base(method)
        {
            this.Method = method;
            this.ContainedMethod = new Method(method.type, methodDef);
            this.Type = method.type;
            this.instruction = instruction;
        }

        internal MethodUsage(Method method, Method containedMethod, Instruction instruction) : base(method)
        {
            this.Method = method;
            this.ContainedMethod = this.ContainedMethod;
            this.Type = method.type;
            this.instruction = instruction;
        }

        public Method ContainedMethod { get; private set; }
        public Method Method { get; private set; }
        public BuilderType Type { get; private set; }

        public BuilderType GetGenericArgument(int index)
        {
            var method = this.instruction.Operand as GenericInstanceMethod;

            if (method == null)
                return null;

            return new BuilderType(this.Method.type.Builder, method.GenericArguments[index]);
        }

        public BuilderType GetPreviousInstructionObjectType()
        {
            TypeReference declaringType = null;
            var previousInstruction = instruction.Previous;

            if (previousInstruction.OpCode == OpCodes.Dup)
                previousInstruction = previousInstruction.Previous;

            if (previousInstruction.Operand is MethodReference)
                declaringType = (previousInstruction.Operand as MethodReference).DeclaringType;
            else if (previousInstruction.OpCode == OpCodes.Ldarg_0 ||
               previousInstruction.OpCode == OpCodes.Ldarg_1 ||
               previousInstruction.OpCode == OpCodes.Ldarg_2 ||
               previousInstruction.OpCode == OpCodes.Ldarg_3 ||
               previousInstruction.OpCode == OpCodes.Ldarg_S)
            {
                TypeReference parameter;

                if (previousInstruction.OpCode == OpCodes.Ldarg_0)
                    parameter = this.ContainedMethod.IsStatic ? this.ContainedMethod.methodReference.Parameters[0].ParameterType : this.ContainedMethod.DeclaringType.typeReference;
                else if (previousInstruction.OpCode == OpCodes.Ldarg_1)
                    parameter = this.ContainedMethod.methodReference.Parameters[this.ContainedMethod.IsStatic ? 1 : 0].ParameterType;
                else if (previousInstruction.OpCode == OpCodes.Ldarg_2)
                    parameter = this.ContainedMethod.methodReference.Parameters[this.ContainedMethod.IsStatic ? 2 : 1].ParameterType;
                else if (previousInstruction.OpCode == OpCodes.Ldarg_3)
                    parameter = this.ContainedMethod.methodReference.Parameters[this.ContainedMethod.IsStatic ? 3 : 2].ParameterType;
                else
                    parameter = this.ContainedMethod.methodReference.Parameters[(int)previousInstruction.Operand].ParameterType;

                declaringType = parameter;
            }
            else if (previousInstruction.OpCode == OpCodes.Ldloc_0 ||
                previousInstruction.OpCode == OpCodes.Ldloc_1 ||
                previousInstruction.OpCode == OpCodes.Ldloc_2 ||
                previousInstruction.OpCode == OpCodes.Ldloc_3 ||
                previousInstruction.OpCode == OpCodes.Ldloc_S)
            {
                VariableReference local;

                if (previousInstruction.OpCode == OpCodes.Ldloc_0)
                    local = this.ContainedMethod.methodDefinition.Body.Variables[0];
                else if (previousInstruction.OpCode == OpCodes.Ldloc_1)
                    local = this.ContainedMethod.methodDefinition.Body.Variables[1];
                else if (previousInstruction.OpCode == OpCodes.Ldloc_2)
                    local = this.ContainedMethod.methodDefinition.Body.Variables[2];
                else if (previousInstruction.OpCode == OpCodes.Ldloc_3)
                    local = this.ContainedMethod.methodDefinition.Body.Variables[3];
                else
                    local = previousInstruction.Operand as VariableReference;

                declaringType = local.VariableType;
            }
            else if (previousInstruction.OpCode == OpCodes.Ldfld || previousInstruction.OpCode == OpCodes.Ldsfld)
            {
                var field = previousInstruction.Operand as FieldReference;
                declaringType = field.FieldType;
            }
            else
            {
                this.LogWarning($"Unable to implement CreateObject<> in '{ this.ContainedMethod.methodDefinition.Name}'. The anonymous type was not found.");
            }

            return new BuilderType(this.Method.type.Builder, declaringType);
        }

        #region Equitable stuff

        public static implicit operator string(MethodUsage value) => value.ToString();

        public static bool operator !=(MethodUsage a, MethodUsage b) => !(a == b);

        public static bool operator ==(MethodUsage a, MethodUsage b)
        {
            if (object.Equals(a, null) && object.Equals(b, null))
                return true;

            if (object.Equals(a, null))
                return false;

            return a.Equals(b);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            if (object.Equals(obj, null))
                return false;

            if (object.ReferenceEquals(obj, this))
                return true;

            if (obj is MethodUsage)
                return this.Equals(obj as MethodUsage);

            return false;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool Equals(MethodUsage other)
        {
            if (object.Equals(other, null))
                return false;

            if (object.ReferenceEquals(other, this))
                return true;

            return object.Equals(other, this);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => this.ContainedMethod.GetHashCode() ^ this.Method.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => $"IL_{this.instruction.Offset.ToString("X4")} >> {this.ContainedMethod.methodDefinition.FullName} >> {this.Method.methodReference.Name}";

        #endregion Equitable stuff
    }
}