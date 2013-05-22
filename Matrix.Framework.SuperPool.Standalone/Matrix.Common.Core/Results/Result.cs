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

namespace Matrix.Common.Core.Results
{
    /// <summary>
    /// Class provides common extended result information, for an operation.
    /// 
    /// *IMPROTANT* if Result is struct, it can never be null so allows automated safety in this direction.
    /// </summary>
    [Serializable]
    public class Result
    {
        public enum ResultEnum
        {
            Success,
            Failure
        };

        /// <summary>
        /// Default predefined value.
        /// </summary>
        public static Result Success
        {
            get { return new Result(ResultEnum.Success); }
        }

        /// <summary>
        /// Default predefined value.
        /// </summary>
        public static Result Failure
        {
            get { return new Result(ResultEnum.Failure); }
        }

        volatile ResultEnum _value;
        /// <summary>
        /// Value of the result.
        /// </summary>
        public ResultEnum Value
        {
            get { return _value; }
            set { _value = value; }
        }

        Exception _optionalException;
        /// <summary>
        /// Exception instance that may have occured during the operation.
        /// </summary>
        public Exception OptionalException
        {
            get { return _optionalException; }
            set { _optionalException = value; }
        }

        volatile string _optionalMessage;
        /// <summary>
        /// Any message from the operation.
        /// </summary>
        public string OptionalMessage
        {
            get { return _optionalMessage; }
            set { _optionalMessage = value; }
        }

        public bool IsSuccess
        {
            get
            {
                return Value == ResultEnum.Success;
            }
        }

        public bool IsFailure
        {
            get
            {
                return Value == ResultEnum.Failure;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public Result(ResultEnum result)
        {
            _optionalMessage = string.Empty;
            _optionalException = null;
            _value = result;
        }

        /// <summary>
        /// Create a success result with this message.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Result Succeed(string message)
        {
            return new Result(ResultEnum.Success) { OptionalMessage = message };
        }

        /// <summary>
        /// Create a fail result with this message.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Result Fail(string message)
        {
            return new Result(ResultEnum.Failure) { OptionalMessage = message };
        }

        /// <summary>
        /// Create a fail result with this message.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Result Fail(string message, Exception exception)
        {
            return new Result(ResultEnum.Failure) { OptionalMessage = message, OptionalException = exception };
        }

        /// <summary>
        /// Create a fail result with this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Result Fail(Exception exception)
        {
            return new Result(ResultEnum.Failure) { OptionalException = exception };
        }

        public override string ToString()
        {
            return base.ToString() + ", Msg[" + OptionalMessage + "], Exc[" + CommonHelper.GetExceptionMessage(OptionalException) + "]";
        }
    }
}
