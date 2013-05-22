// -----
// Copyright 2010 Deyan Timnev
// This file is part of the Matrix Platform (www.matrixplatform.com).
// The Matrix Platform is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version. The Matrix Platform is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with the Matrix Platform. If not, see http://www.gnu.org/licenses/lgpl.html
// -----
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Threading;
using Matrix.Framework.SuperPool.DynamicProxy;

namespace Matrix.Framework.SuperPool.UnitTest
{
    public interface ISampleFace
    {
        void TestMethod(int param1, object param2);
        ProxyTest TestMethod2(int param1, object param2);

        event EventHandler SuperEvent1;
    }

    public class ProxyTest : ISampleFace
    {
        IProxyTypeSink _sink = null;

        public int MyProperty
        {
            get
            {
                return (int)_sink.ReceivePropertyGet(3, typeof(int));
            }

            set
            {
                _sink.ReceivePropertySet(4, value);
            }
        }

        public event EventHandler SuperEvent1
        {
            add
            {
                // Safe to do it in runtime since it is fairly fast.
                _sink.ReceiveEventSubscribed(2, value);
            }

            remove
            {
                // Safe to do it in runtime since it is fairly fast.
                _sink.ReceiveEventUnSubscribed(3, value);
            }
        }

        public void TestMethod(int param1, object param2)
        {
            object[] parameters = new object[2];
            parameters[0] = param1;
            parameters[1] = param2;

            _sink.ReceiveMethodCall(35, parameters);
        }

        #region ISampleFace Members

        public ProxyTest TestMethod2(int param1, object param2)
        {
            return (ProxyTest)_sink.ReceiveMethodCallAndReturn(36, typeof(string), null);
        }

        #endregion
    }

    class Program : IProxyTypeSink
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ProxyTest test = new ProxyTest();
            test.SuperEvent1 += new EventHandler(test_SuperEvent1);

            Program sink = new Program();

            ProxyTypeBuilder builder = new ProxyTypeBuilder("ProxyBuilder");
            //Type newType = builder.GenerateInterfaceProxyImplementation<ISampleFace>();
            ISampleFace proxy = builder.ObtainProxyInstance<ISampleFace>(sink);

            Stopwatch sw = new Stopwatch();
            Thread.Sleep(500);
            sw.Start();
            for (int i = 0; i < 10000000; i++)
            {
                proxy.TestMethod(12, null);
                object t = proxy.TestMethod2(13, sink);
            }

            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
            //builder.Save();

            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());
        }

        static void test_SuperEvent1(object sender, EventArgs e)
        {

        }

        #region IProxyTypeSink Members

        int x;
        public void ReceiveMethodCall(int methodId, object[] parameters)
        {
            x = methodId;
        }

        public object ReceiveMethodCallAndReturn(int methodId, Type returnType, object[] parameters)
        {
            return new ProxyTest();
        }

        public void ReceiveEventSubscribed(int methodId, Delegate subscribedDelegate)
        {

        }

        public void ReceiveEventUnSubscribed(int methodId, Delegate subscribedDelegate)
        {

        }

        #endregion

        // An assembly consists of one or more modules, each of which
        // contains zero or more types. This code creates a single-module
        // assembly, the most common case. The module contains one type,
        // named "MyDynamicType", that has a private field, a property 
        // that gets and sets the private field, constructors that 
        // initialize the private field, and a method that multiplies 
        // a user-supplied number by the private field value and returns
        // the result. In C# the type might look like this:
        /*
        public class MyDynamicType
        {
            private int m_number;

            public MyDynamicType() : this(42) {}
            public MyDynamicType(int initNumber)
            {
                m_number = initNumber;
            }

            public int Number
            {
                get { return m_number; }
                set { m_number = value; }
            }

            public int MyMethod(int multiplier)
            {
                return m_number * multiplier;
            }
        }
        */

        public static void Main3()
        {
            AssemblyName aName = new AssemblyName("DynamicAssemblyExample");
            AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndSave);
            // For a single-module assembly, the module name is usually the assembly name plus an extension.
            ModuleBuilder mb = ab.DefineDynamicModule(aName.Name, aName.Name + ".dll");

            TypeBuilder tb = mb.DefineType("MyDynamicType", TypeAttributes.Public);

            //// Add a private field of type int (Int32).
            //FieldBuilder fbNumber = tb.DefineField("m_number", typeof(int), FieldAttributes.Private);

            //// Define a constructor that takes an integer argument and 
            //// stores it in the private field. 
            //Type[] parameterTypes = { typeof(int) };
            //ConstructorBuilder ctor1 = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes);

            //#region Constructor 1

            //ILGenerator ctor1IL = ctor1.GetILGenerator();
            //// For a constructor, argument zero is a reference to the new
            //// instance. Push it on the stack before calling the base
            //// class constructor. Specify the default constructor of the 
            //// base class (System.Object) by passing an empty array of 
            //// types (Type.EmptyTypes) to GetConstructor.
            //ctor1IL.Emit(OpCodes.Ldarg_0);
            //ctor1IL.Emit(OpCodes.Call,
            //    typeof(object).GetConstructor(Type.EmptyTypes));
            //// Push the instance on the stack before pushing the argument
            //// that is to be assigned to the private field m_number.
            //ctor1IL.Emit(OpCodes.Ldarg_0);
            //ctor1IL.Emit(OpCodes.Ldarg_1);
            //ctor1IL.Emit(OpCodes.Stfld, fbNumber);
            //ctor1IL.Emit(OpCodes.Ret);

            //#endregion

            //#region Constructor 2

            //// Define a default constructor that supplies a default value
            //// for the private field. For parameter types, pass the empty
            //// array of types or pass null.
            //ConstructorBuilder ctor0 = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);

            //ILGenerator ctor0IL = ctor0.GetILGenerator();
            //// For a constructor, argument zero is a reference to the new
            //// instance. Push it on the stack before pushing the default
            //// value on the stack, then call constructor ctor1.
            //ctor0IL.Emit(OpCodes.Ldarg_0);
            //ctor0IL.Emit(OpCodes.Ldc_I4_S, 42);
            //ctor0IL.Emit(OpCodes.Call, ctor1);
            //ctor0IL.Emit(OpCodes.Ret);

            //#endregion

            //#region Set and Get
            //// Define a property named Number that gets and sets the private 
            //// field.
            ////
            //// The last argument of DefineProperty is null, because the
            //// property has no parameters. (If you don't specify null, you must
            //// specify an array of Type objects. For a parameterless property,
            //// use the built-in array with no elements: Type.EmptyTypes)
            //PropertyBuilder pbNumber = tb.DefineProperty(
            //    "Number",
            //    PropertyAttributes.HasDefault,
            //    typeof(int),
            //    null);

            //// The property "set" and property "get" methods require a special
            //// set of attributes.
            //MethodAttributes getSetAttr = MethodAttributes.Public |
            //    MethodAttributes.SpecialName | MethodAttributes.HideBySig;

            //// Define the "get" accessor method for Number. The method returns
            //// an integer and has no arguments. (Note that null could be 
            //// used instead of Types.EmptyTypes)
            //MethodBuilder mbNumberGetAccessor = tb.DefineMethod(
            //    "get_Number",
            //    getSetAttr,
            //    typeof(int),
            //    Type.EmptyTypes);

            //ILGenerator numberGetIL = mbNumberGetAccessor.GetILGenerator();
            //// For an instance property, argument zero is the instance. Load the 
            //// instance, then load the private field and return, leaving the
            //// field value on the stack.
            //numberGetIL.Emit(OpCodes.Ldarg_0);
            //numberGetIL.Emit(OpCodes.Ldfld, fbNumber);
            //numberGetIL.Emit(OpCodes.Ret);

            //// Define the "set" accessor method for Number, which has no return
            //// type and takes one argument of type int (Int32).
            //MethodBuilder mbNumberSetAccessor = tb.DefineMethod(
            //    "set_Number",
            //    getSetAttr,
            //    null,
            //    new Type[] { typeof(int) });

            //ILGenerator numberSetIL = mbNumberSetAccessor.GetILGenerator();
            //// Load the instance and then the numeric argument, then store the
            //// argument in the field.
            //numberSetIL.Emit(OpCodes.Ldarg_0);
            //numberSetIL.Emit(OpCodes.Ldarg_1);
            //numberSetIL.Emit(OpCodes.Stfld, fbNumber);
            //numberSetIL.Emit(OpCodes.Ret);

            //// Last, map the "get" and "set" accessor methods to the 
            //// PropertyBuilder. The property is now complete. 
            //pbNumber.SetGetMethod(mbNumberGetAccessor);
            //pbNumber.SetSetMethod(mbNumberSetAccessor);

            //#endregion


            //#region Integer setting method

            ////Define a method that accepts an integer argument and returns
            //// the product of that integer and the private field m_number. This
            //// time, the array of parameter types is created on the fly.
            //MethodBuilder meth = tb.DefineMethod("MyMethod", MethodAttributes.Public, typeof(int), new Type[] { typeof(int) });

            //ILGenerator methIL = meth.GetILGenerator();
            //// To retrieve the private instance field, load the instance it
            //// belongs to (argument zero). After loading the field, load the 
            //// argument one and then multiply. Return from the method with 
            //// the return value (the product of the two numbers) on the 
            //// execution stack.
            //methIL.Emit(OpCodes.Ldarg_0);
            //methIL.Emit(OpCodes.Ldfld, fbNumber);
            //methIL.Emit(OpCodes.Ldarg_1);
            //methIL.Emit(OpCodes.Mul);
            //methIL.Emit(OpCodes.Ret);

            //#endregion










            // Finish the type.
            Type t = tb.CreateType();

            // The following line saves the single-module assembly. This
            // requires AssemblyBuilderAccess to include Save. You can now
            // type "ildasm MyDynamicAsm.dll" at the command prompt, and 
            // examine the assembly. You can also write a program that has
            // a reference to the assembly, and use the MyDynamicType type.
            // 
            ab.Save(aName.Name + ".dll");

            // Because AssemblyBuilderAccess includes Run, the code can be
            // executed immediately. Start by getting reflection objects for
            // the method and the property.
            MethodInfo mi = t.GetMethod("MyMethod");
            PropertyInfo pi = t.GetProperty("Number");

            //// Create an instance of MyDynamicType using the default 
            //// constructor. 
            //object o1 = Activator.CreateInstance(t);

            //// Display the value of the property, then change it to 127 and 
            //// display it again. Use null to indicate that the property
            //// has no index.
            //Console.WriteLine("o1.Number: {0}", pi.GetValue(o1, null));
            //pi.SetValue(o1, 127, null);
            //Console.WriteLine("o1.Number: {0}", pi.GetValue(o1, null));

            //// Call MyMethod, passing 22, and display the return value, 22
            //// times 127. Arguments must be passed as an array, even when
            //// there is only one.
            //object[] arguments = { 22 };
            //Console.WriteLine("o1.MyMethod(22): {0}",
            //    mi.Invoke(o1, arguments));

            //// Create an instance of MyDynamicType using the constructor
            //// that specifies m_Number. The constructor is identified by
            //// matching the types in the argument array. In this case, 
            //// the argument array is created on the fly. Display the 
            //// property value.
            //object o2 = Activator.CreateInstance(t,
            //    new object[] { 5280 });
            //Console.WriteLine("o2.Number: {0}", pi.GetValue(o2, null));
        }


        #region IProxyTypeSink Members


        public object ReceiveDynamicMethodCallAndReturn(int methodId, object source, object[] parameters, Type returnType)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IProxyTypeSink Members


        public void ReceiveDynamicMethodCall(int methodClassId, object source, object[] parameters, Type returnType)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IProxyTypeSink Members


        public object ReceivePropertyGet(int methodId, Type returnType)
        {
            throw new NotImplementedException();
        }

        public void ReceivePropertySet(int methodId, object value)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
