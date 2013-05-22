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
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using Matrix.Common.Core;
using Matrix.Framework.MessageBus.Core;

namespace Matrix.Framework.MessageBus.Net.Messages
{
    /// <summary>
    /// Message contains info on a set of message bus clients.
    /// </summary>
    [Serializable]
    internal class ClientsListMessage : Message, IDeserializationCallback
    {
        public List<ClientId> Ids = new List<ClientId>();

        /// <summary>
        /// Over the network - transport only types names, since otherwise an 
        /// uknown types causes exceptions on the very message.
        /// </summary>
        List<string> _types = new List<string>();
        List<List<string>> _sourcesTypesNames = new List<List<string>>();

        [NonSerialized]
        List<Type> _typesLocal = new List<Type>();
        public ReadOnlyCollection<Type> Types
        {
            get
            {
                return _typesLocal.AsReadOnly();
            }
        }

        public ReadOnlyCollection<List<string>> SourcesTypes
        {
            get
            {
                return _sourcesTypesNames.AsReadOnly();
            }
        }

        public void AddType(Type type, Type sourceType)
        {
            lock (this)
            {
                _types.Add(type.AssemblyQualifiedName);
                _sourcesTypesNames.Add(ReflectionHelper.GetTypeNameAndRelatedTypes(sourceType));
            }
        }

        #region IDeserializationCallback Members

        public void OnDeserialization(object sender)
        {
            _typesLocal = new List<Type>();

            for (int i = 0; i < _types.Count; i++)
            {
                Type type = Type.GetType(_types[i]);

                if (type != null)
                {
                    lock (this)
                    {
                        _typesLocal.Add(type);
                        //_sourcesTypesLocal.Add(sourceType);
                    }
                }
            }
        }

        #endregion
    }
}
