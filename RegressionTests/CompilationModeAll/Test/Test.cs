﻿using Junit.Framework;

namespace CompilationModeAll
{
    public class Test : TestCase
    {
        public void testUnreferenced()
        {
			var cl = GetType().GetClassLoader();
            var type = cl.LoadClass("compilationModeAll.Unreferenced");
            AssertNotNull(type);
            AssertEquals(1, type.GetDeclaredMethods().Length);
            AssertEquals(2, type.GetDeclaredFields().Length);
        }

        public void testLib1()
        {
			var cl = GetType().GetClassLoader();
            var type = cl.LoadClass("lib1.Class1");
            AssertNotNull(type);
            AssertEquals(1, type.GetDeclaredMethods().Length);
            AssertEquals(2, type.GetDeclaredFields().Length);
        }

        public void testLib2()
        {
			var cl = GetType().GetClassLoader();
			System.Type type = null;
			try {
				type = cl.LoadClass("lib2.Class1");
			} catch (System.Exception ex) {
			}
            AssertNull(type);
        }
    }
	
	internal class Unreferenced {
        private static int staticField;
        private int instanceField;
        private void PrivateFoo() { } 
	}
}
