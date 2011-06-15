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

using Autofac;
using Autofac.Core;

using NHibernate;
using NHibernate.ByteCode.Castle;
using NHibernate.Cache;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Tool.hbm2ddl;

using Rhino.Security;
using Rhino.Security.Interfaces;
using Rhino.Security.Services;

using Environment = NHibernate.Cfg.Environment;

namespace Lokad.Cqrs.Extensions.Permissions.Build
{
    public class PermissionsModule : IModule
    {
        private readonly ContainerBuilder builder;
        private readonly Filter<Type> entityFilter;
        private readonly InformationExtractorModule informationExtractorModule;
        protected Action<Configuration> ConfigureSecurity;
        protected string DataConnectionString = ".";
        protected Type DataDialect;
        protected Type DataDriver;
        private bool initDb;

        public PermissionsModule()
        {
            builder = new ContainerBuilder();

            informationExtractorModule = new InformationExtractorModule();
            builder.RegisterModule(informationExtractorModule);

            entityFilter = new Filter<Type>();
            entityFilter.Where(t => t.IsAssignableTo<ISecurableEntity>() && t.IsClass && !t.IsAbstract);
        }

        #region IModule Members

        public virtual void Configure(IComponentRegistry componentRegistry)
        {
            builder.RegisterType<AuthorizationService>()
                .InstancePerLifetimeScope()
                .As<IAuthorizationService>();

            builder.RegisterType<AuthorizationRepository>()
                .InstancePerLifetimeScope()
                .As<IAuthorizationRepository>();

            builder.RegisterType<PermissionsBuilderService>()
                .InstancePerLifetimeScope()
                .As<IPermissionsBuilderService>();

            builder.RegisterType<PermissionsService>()
                .InstancePerLifetimeScope()
                .As<IPermissionsService>();

            builder.Register(ComposeSessionFactory)
                .SingleInstance()
                .As<ISessionFactory>();

            builder.Register(OpenSession).InstancePerLifetimeScope()
                .As<ISession>();

            builder.RegisterType<ServiceLocatorSetter>()
                .SingleInstance()
                .As<IStartable>();
            
            builder.Update(componentRegistry);
        }

        #endregion

        public PermissionsModule InformationExtractors(Action<InformationExtractorModule> config)
        {
            config(informationExtractorModule);
            return this;
        }

        public void InitializeDatabase()
        {
            initDb = true;
        }

        public PermissionsModule Persistance<TDriver, TDialect>(string connectionString)
            where TDriver : IDriver
            where TDialect : Dialect
        {
            DataConnectionString = connectionString;
            DataDialect = typeof (TDialect);
            DataDriver = typeof (TDriver);

            return this;
        }

        public PermissionsModule UserIs<T>() where T : IUser
        {
            ConfigureSecurity = config =>
            {
                config.AddAssembly(typeof (T).Assembly);
                Security.Configure<T>(config, SecurityTableStructure.Prefix);
            };
            return this;
        }

        protected virtual ISession OpenSession(IComponentContext context)
        {
            var factory = context.Resolve<ISessionFactory>();
            return factory.OpenSession();
        }

        protected virtual ISessionFactory ComposeSessionFactory(IComponentContext context)
        {
            Configuration config = new Configuration()
                .SetProperty(Environment.ConnectionDriver, DataDriver.AssemblyQualifiedName)
                .SetProperty(Environment.Dialect, DataDialect.AssemblyQualifiedName)
                .SetProperty(Environment.ConnectionString, DataConnectionString)
                .SetProperty(Environment.ProxyFactoryFactoryClass, typeof (ProxyFactoryFactory).AssemblyQualifiedName)
                .SetProperty(Environment.ReleaseConnections, "on_close")
                .SetProperty(Environment.UseSecondLevelCache, "true")
                .SetProperty(Environment.UseQueryCache, "true")
                .SetProperty(Environment.CacheProvider, typeof (HashtableCacheProvider).AssemblyQualifiedName);

            ConfigureSecurity(config);

            ISessionFactory factory = config.BuildSessionFactory();

            if(initDb)
            {
                using (ISession session = factory.OpenSession())
                {
                    new SchemaExport(config).Execute(false, true, false, session.Connection, null);
                }
                
            }

            return factory;
        }
    }
}