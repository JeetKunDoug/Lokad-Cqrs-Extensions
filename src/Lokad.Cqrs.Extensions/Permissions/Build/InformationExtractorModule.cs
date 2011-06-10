#region Copyright (c) 2011, EventDay Inc.

// Copyright (c) 2011, EventDay Inc.
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of the EventDay Inc. nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL EventDay Inc. BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Autofac;
using Autofac.Core;

using Rhino.Security.Interfaces;

namespace Lokad.Cqrs.Extensions.Permissions.Build
{
    public class InformationExtractorModule : IModule
    {
        private readonly HashSet<Assembly> assemblies;
        private readonly ContainerBuilder builder;
        private readonly Filter<Type> entityFilter;

        public InformationExtractorModule()
        {
            builder = new ContainerBuilder();
            assemblies = new HashSet<Assembly>();

            entityFilter =
                new Filter<Type>().Where(t => t.IsAssignableTo<ISecurableEntity>() && t.IsClass && !t.IsAbstract);
        }

        #region IModule Members

        void IModule.Configure(IComponentRegistry componentRegistry)
        {
            ConfigureInformationExtractors();

            builder.Update(componentRegistry);
        }

        #endregion

        public InformationExtractorModule EntitiesAreInAssemblyOf<T>()
        {
            assemblies.Add(typeof (T).Assembly);
            return this;
        }

        private void ConfigureInformationExtractors()
        {
            var types = assemblies.SelectMany(a => a.GetExportedTypes());

            ConfigureDiscoveredEntityExtractors(entityFilter.Apply(types).ToArray());
        }

        private void ConfigureDiscoveredEntityExtractors(IEnumerable<Type> entities)
        {
            var extractorBaseType = typeof (IEntityInformationExtractor<>);
            var extractorType = typeof (EntityInformationExtractor<>);

            List<Type> list = entities.ToList();

            list.ForEach(t =>
            {
                var service = extractorBaseType.MakeGenericType(t);
                var type = extractorType.MakeGenericType(t);

                builder.RegisterType(type).SingleInstance().As(service);
            });
        }
    }
}