// -----
// Copyright 2010 Deyan Timnev
// This file is part of the Matrix Platform (www.matrixplatform.com).
// The Matrix Platform is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version. The Matrix Platform is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with the Matrix Platform. If not, see http://www.gnu.org/licenses/lgpl.html
// -----
using System;

namespace Matrix.Common.Core
{
    /// <summary>
    /// Serves to receive and process system monitoring operation on runtime.
    /// A simplified version of the System monitor, allows the core components
    /// to have some primitive access to this type of functionality as well.
    /// </summary>
    public static class CoreSystemMonitor
    {
        public delegate void ReportDelegate(string details, Exception optionalException);

        public static event ReportDelegate InfoEvent;
        public static event ReportDelegate OperationErrorEvent;
        public static event ReportDelegate OperationWarningEvent;
        public static event ReportDelegate ErrorEvent;
        public static event ReportDelegate WarningEvent;

        internal static void Info(string errorMessage)
        {
            ReportDelegate del = InfoEvent;
            if (del != null)
            {
                del(errorMessage, null);
            }
        }

        internal static void OperationError(string errorDetails)
        {
            ReportDelegate del = OperationErrorEvent;
            if (del != null)
            {
                del(errorDetails, null);
            }
        }

        internal static void OperationError(string errorDetails, Exception exception)
        {
            ReportDelegate del = OperationErrorEvent;
            if (del != null)
            {
                del(errorDetails, exception);
            }
        }

        internal static void OperationWarning(string warningMessage)
        {
            ReportDelegate del = OperationWarningEvent;
            if (del != null)
            {
                del(warningMessage, null);
            }
        }

        internal static void Error(string errorMessage)
        {
            ReportDelegate del = ErrorEvent;
            if (del != null)
            {
                del(errorMessage, null);
            }
        }

        internal static void Error(string errorMessage, Exception exception)
        {
            ReportDelegate del = ErrorEvent;
            if (del != null)
            {
                del(errorMessage, exception);
            }
        }

        internal static void Warning(string warningMessage)
        {
            ReportDelegate del = WarningEvent;
            if (del != null)
            {
                del(warningMessage, null);
            }
        }
    }
}
