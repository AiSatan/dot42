﻿using System.ComponentModel.Composition;
using Dot42.ImportJarLib;
using Dot42.ImportJarLib.Mapped;
using Dot42.JvmClassLib;

namespace Dot42.FrameworkBuilder.Mapped
{
    /// <summary>
    /// Helper used to build type definitions from ClassFile's
    /// </summary>
    [Export(typeof(IMappedTypeBuilder))]
    internal sealed class JavaLangReflectFieldBuilder: JavaBaseTypeBuilder
    {
        /// <summary>
        /// Empty ctor
        /// </summary>
        public JavaLangReflectFieldBuilder() : this(ClassFile.Empty) { }

        /// <summary>
        /// Default ctor
        /// </summary>
        internal JavaLangReflectFieldBuilder(ClassFile cf)
            : base(cf, "System.Reflection.FieldInfo", "java/lang/reflect/Field")
        {
        }

        /// <summary>
        /// Modify the name of the given method to another name.
        /// By calling renamer.Rename, all methods in the same group are also updated.
        /// </summary>
        public override void ModifyMethodName(ImportJarLib.Model.NetMethodDefinition method, MethodRenamer renamer)
        {
            base.ModifyMethodName(method, renamer);
            if (method.OriginalJavaName == "get")
                renamer.Rename(method, "GetValue");
        }
    }
}
