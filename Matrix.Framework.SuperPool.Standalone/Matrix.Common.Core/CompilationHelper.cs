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
using System.Text;
using Matrix.Common.Core;
using Microsoft.CSharp;
using System.Reflection;
using System.CodeDom.Compiler;

namespace Matrix.Common.Core
{
    /// <summary>
    /// Helper class, helps with the runtime compilation operations.
    /// </summary>
    public static class CompilationHelper
    {
        /// <summary>
        ///  This will compile sources using the tools integrated in the .NET framework.
        /// </summary>
        /// <param name="sourceCode"></param>
        /// <param name="resultMessagesAndRows"></param>
        /// <returns>Will return null if compilation was not successful.</returns>
        public static Assembly CompileSourceToAssembly(string sourceCode, out Dictionary<string, int> resultMessagesAndRows)
        {
            resultMessagesAndRows = new Dictionary<string, int>();

            CSharpCodeProvider codeProvider = new CSharpCodeProvider();

            System.CodeDom.Compiler.CompilerParameters parameters = new CompilerParameters();
            parameters.GenerateInMemory = true;

            // this will add references to all the assemblies we are using.
            foreach (Assembly assembly in ReflectionHelper.GetAssemblies(true, true))
            {
                parameters.ReferencedAssemblies.Add(assembly.Location);
            }

            CompilerResults results = codeProvider.CompileAssemblyFromSource(parameters, sourceCode);
            
            if (results.Errors.Count > 0)
            {
                foreach (CompilerError error in results.Errors)
                {
                    resultMessagesAndRows.Add("Line " + error.Line.ToString() + ": (" + error.ErrorNumber.ToString() + ")" + error.ErrorText, error.Line);
                }
                return null;
            }
            else
            {
                resultMessagesAndRows.Add("Compiled succesfully", -1);
                return results.CompiledAssembly;
            }
        }

        //This is a compilation done the CSScriptLibrary way
        //try
        //{
        //    Assembly assembly = CSScriptLibrary.CSScript.LoadCode(source, null, true);
        //    ListViewItem item2 = new ListViewItem("Compilation Succesfull", "ok");
        //    this.listView1.Items.Add(item2);
        //    return assembly;
        //}
        //catch (Exception ex)
        //{
        //    foreach(String line in ex.Message.Split(new char[] {}))
        //    {
        //        ListViewItem item = new ListViewItem(line, "error");
        //        this.listView1.Items.Add(item);
        //    }
        //    return null;
        //}


    }
}
