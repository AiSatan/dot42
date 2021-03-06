﻿using System.Collections.Generic;
using System.Linq;
using Dot42.CompilerLib.XModel;
using Dot42.FrameworkDefinitions;
using Dot42.Utility;

namespace Dot42.CompilerLib.Ast.Converters
{
    /// <summary>
    /// Add/expand generic instance type information
    /// </summary>
    internal static class GenericInstanceConverter 
    {
        /// <summary>
        /// Optimize expressions
        /// </summary>
        public static void Convert(AstNode ast, MethodSource currentMethod, AssemblyCompiler compiler)
        {
            var typeSystem = compiler.Module.TypeSystem;

            // Expand typeof
            foreach (var node in ast.GetSelfAndChildrenRecursive<AstExpression>(x => x.Code == AstCode.TypeOf))
            {
                var type = (XTypeReference) node.Operand;
                var typeHelperType = compiler.GetDot42InternalType(InternalConstants.TypeHelperName).Resolve();
                var loadExpr = LoadTypeForGenericInstance(node.SourceLocation, currentMethod, type, typeHelperType, typeSystem, false);
                node.CopyFrom(loadExpr);
            }

            // Expand newarr
            foreach (var node in ast.GetSelfAndChildrenRecursive<AstExpression>(x => x.Code == AstCode.Newarr))
            {
                var type = (XTypeReference)node.Operand;
                if (!type.IsDefinitionOrReferenceOrPrimitive())
                {
                    // Resolve type to a Class<?>
                    var typeHelperType = compiler.GetDot42InternalType(InternalConstants.TypeHelperName).Resolve();
                    var ldType = LoadTypeForGenericInstance(node.SourceLocation, currentMethod, type, typeHelperType, typeSystem, false);
                    var newInstanceExpr = new AstExpression(node.SourceLocation, AstCode.ArrayNewInstance, null, ldType, node.Arguments[0]) { ExpectedType = typeSystem.Object };
                    var arrayType = new XArrayType(type);
                    var cast = new AstExpression(node.SourceLocation, AstCode.SimpleCastclass, arrayType, newInstanceExpr) { ExpectedType = arrayType };
                    node.CopyFrom(cast);
                }
            }

            // Add generic instance call arguments
            foreach (var node in ast.GetSelfAndChildrenRecursive<AstExpression>(x => x.Code.IsCall()))
            {
                var method = (XMethodReference)node.Operand;
                if (method.DeclaringType.IsArray)
                    continue;
                XMethodDefinition methodDef;
                if (!method.TryResolve(out methodDef)) 
                    continue;
                if (methodDef.HasDexNativeAttribute())
                    continue;

                if (methodDef.NeedsGenericInstanceTypeParameter)
                {
                    // Add generic instance type parameter value
                    var arg = CreateGenericInstance(node.SourceLocation, method.DeclaringType, currentMethod, compiler);
                    node.Arguments.Add(arg);
                    node.GenericInstanceArgCount++;
                }

                if (methodDef.NeedsGenericInstanceMethodParameter)
                {
                    // Add generic instance method parameter
                    var arg = CreateGenericInstance(node.SourceLocation, method, currentMethod, compiler);
                    node.Arguments.Add(arg);
                    node.GenericInstanceArgCount++;
                }
            }

            // Convert NewObj when needed
            foreach (var node in ast.GetSelfAndChildrenRecursive<AstExpression>(x => x.Code == AstCode.Newobj))
            {
                var ctorRef = (XMethodReference)node.Operand;
                var declaringType = ctorRef.DeclaringType;
                if (declaringType.IsArray)
                {
                    // New multi dimensional array
                    // Get element type
                    var elemType = ((XArrayType) declaringType).ElementType;
                    var typeExpr = new AstExpression(node.SourceLocation, AstCode.TypeOf, elemType);

                    // Create dimensions array
                    var intArrayType = new XArrayType(typeSystem.Int);
                    var dimArrayExpr = new AstExpression(node.SourceLocation, AstCode.InitArrayFromArguments, intArrayType, node.Arguments).SetType(intArrayType);

                    // Call java.lang.reflect.Array.newInstance(type, int[])
                    var newInstanceExpr = new AstExpression(node.SourceLocation, AstCode.ArrayNewInstance2, null, typeExpr, dimArrayExpr).SetType(typeSystem.Object);

                    // Cast to correct type
                    var cast = new AstExpression(node.SourceLocation, AstCode.SimpleCastclass, declaringType, newInstanceExpr).SetType(declaringType);

                    // Replace node
                    node.CopyFrom(cast);
                }
                else
                {
                    // Normal "new object"
                    XMethodDefinition ctorDef;
                    if (ctorRef.TryResolve(out ctorDef) && ctorDef.NeedsGenericInstanceTypeParameter)
                    {
                        // Add generic instance type parameter value
                        var arg = CreateGenericInstance(node.SourceLocation, ctorRef.DeclaringType, currentMethod, compiler);
                        node.Arguments.Add(arg);
                        node.GenericInstanceArgCount++;
                    }
                }
            }
        }

        /// <summary>
        /// Build expression that creates an instance of GenericInstance with arguments from the given .NET generic instance.
        /// </summary>
        private static AstExpression CreateGenericInstance(ISourceLocation seqp, XReference member, MethodSource currentMethod, AssemblyCompiler compiler)
        {
            // Prepare
            var genericInstance = member as IXGenericInstance;
            if (genericInstance == null)
            {
#if DEBUG
                //Debugger.Launch();
#endif
                throw new CompilerException(string.Format("{0} is not a generic instance", member));
            }
            var count = genericInstance.GenericArguments.Count;
            var typeHelperType = compiler.GetDot42InternalType(InternalConstants.TypeHelperName).Resolve();

            // Foreach type argument
            var typeExpressions = new List<AstExpression>();
            for (var i = 0; i < count; i++)
            {
                var argType = genericInstance.GenericArguments[i];
                var typeExpr = LoadTypeForGenericInstance(seqp, currentMethod, argType, typeHelperType, compiler.Module.TypeSystem);
                typeExpressions.Add(typeExpr);
            }

            var elementType = compiler.Module.TypeSystem.Type;
            return new AstExpression(seqp, AstCode.InitArrayFromArguments, new XArrayType(elementType), typeExpressions) { ExpectedType = new XArrayType(elementType) };
        }

        /// <summary>
        /// Create an expression that loads the given type at runtime.
        /// </summary>
        private static AstExpression LoadTypeForGenericInstance(ISourceLocation seqp, MethodSource currentMethod, XTypeReference type, XTypeDefinition typeHelperType, XTypeSystem typeSystem, bool boxPrimitiveTypes = true)
        {
            if (type.IsArray)
            {
                // Array type
                var arrayType = (XArrayType)type;
                // Load element type
                var prefix = LoadTypeForGenericInstance(seqp, currentMethod, ((XArrayType)type).ElementType, typeHelperType, typeSystem, boxPrimitiveTypes);
                // Convert to array type
                if (arrayType.Dimensions.Count() == 1)
                {
                    var giCreateArray = typeHelperType.Methods.Single(x => (x.Name == "Array") && (x.Parameters.Count == 1));
                    return new AstExpression(seqp, AstCode.Call, giCreateArray, prefix) { ExpectedType = typeSystem.Type };
                }
                else
                {
                    var giCreateArray = typeHelperType.Methods.Single(x => (x.Name == "Array") && (x.Parameters.Count == 2));
                    var dimensionsExpr = new AstExpression(seqp, AstCode.Ldc_I4, arrayType.Dimensions.Count()) { ExpectedType = typeSystem.Int };
                    return new AstExpression(seqp, AstCode.Call, giCreateArray, prefix, dimensionsExpr) { ExpectedType = typeSystem.Type };
                }
            }

            var gp = type as XGenericParameter;
            if (gp != null)
            {
                AstExpression gi;
                if (gp.Owner is XTypeReference)
                {
                    // Class type parameter
                    var owner = (XTypeReference)gp.Owner;
                    if (owner.GetElementType().Resolve().HasDexImportAttribute())
                    {
                        // Imported type
                        return new AstExpression(seqp, AstCode.TypeOf, typeSystem.Object) { ExpectedType = typeSystem.Type };
                    }
                    if (currentMethod.IsClassCtor)
                    {
                        // Class ctor's cannot have type information.
                        // Return Object instead
                        DLog.Warning(DContext.CompilerCodeGenerator, "Class constructor of {0} tries to use generic parameter. This will always yield Object.", currentMethod.DeclaringTypeFullName);
                        return new AstExpression(seqp, AstCode.TypeOf, typeSystem.Object) { ExpectedType = typeSystem.Type };
                    }
                    gi = currentMethod.IsStatic ?
                        LoadStaticClassGenericInstance(seqp, typeSystem) :
                        LoadInstanceClassGenericInstance(seqp, typeSystem);
                }
                else
                {
                    // Method type parameter
                    var owner = (XMethodReference)gp.Owner;
                    if (owner.GetElementMethod().Resolve().DeclaringType.HasDexImportAttribute())
                    {
                        // Imported type
                        return LoadTypeForGenericInstance(seqp, currentMethod, type.Module.TypeSystem.Object, typeHelperType, typeSystem, boxPrimitiveTypes);
                    }
                    gi = LoadMethodGenericInstance(seqp, typeSystem);
                }

                var indexExpr = new AstExpression(seqp, AstCode.Ldc_I4, gp.Position) { ExpectedType = typeSystem.Int };
                return new AstExpression(seqp, AstCode.Ldelem_Ref, null,
                    gi, indexExpr) { ExpectedType = typeSystem.Type };
            }
            
            if (type is XTypeSpecification)
            {
                // Just use the element type
                var typeSpec = (XTypeSpecification)type;
                return LoadTypeForGenericInstance(seqp, currentMethod, typeSpec.ElementType, typeHelperType, typeSystem, boxPrimitiveTypes);
            }

            if (type.IsPrimitive && boxPrimitiveTypes)
            {
                return new AstExpression(seqp, AstCode.BoxedTypeOf, type) { ExpectedType = typeSystem.Type };
            }
            
            // Plain type reference or definition
            return new AstExpression(seqp, AstCode.TypeOf, type) { ExpectedType = typeSystem.Type };
        }

        /// <summary>
        /// Load the GenericInstance of the current instance.
        /// The result is a temporary register.
        /// </summary>
        private static AstExpression LoadInstanceClassGenericInstance(ISourceLocation seqp, XTypeSystem typeSystem)
        {
            return new AstExpression(seqp, AstCode.LdGenericInstanceField, null) { ExpectedType = new XArrayType(typeSystem.Type) };
        }

        /// <summary>
        /// Load the GenericInstance of the current static method.
        /// The result is a register that cannot be destroyed.
        /// </summary>
        private static AstExpression LoadStaticClassGenericInstance(ISourceLocation seqp, XTypeSystem typeSystem)
        {
            return new AstExpression(seqp, AstCode.LdGenericInstanceTypeArgument, null) { ExpectedType = new XArrayType(typeSystem.Type) };
        }

        /// <summary>
        /// Load the GenericInstance of the current generic method.
        /// The result is a register that cannot be destroyed.
        /// </summary>
        private static AstExpression LoadMethodGenericInstance(ISourceLocation seqp, XTypeSystem typeSystem)
        {
            return new AstExpression(seqp, AstCode.LdGenericInstanceMethodArgument, null) { ExpectedType = new XArrayType(typeSystem.Type) };
        }
    }
}
