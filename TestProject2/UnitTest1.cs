using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using Autofac.Core.Resolving.Pipeline;
using log4net;
using log4net.Config;
using Microsoft.VisualStudio.TestTools.UnitTesting;

//[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]
namespace TestProject1
{
    public class TestMiddlewareModule : Autofac.Module
    {
        private readonly IResolveMiddleware middleware;

        public TestMiddlewareModule(IResolveMiddleware middleware)
        {
            this.middleware = middleware;
        }

        protected override void AttachToComponentRegistration(IComponentRegistryBuilder componentRegistryBuilder,
            IComponentRegistration registration)
        {
            // Attach to the registration's pipeline build.
            registration.PipelineBuilding += (sender, pipeline) =>
            {
                // Add our middleware to the pipeline.
                pipeline.Use(middleware);
            };
        }
    }

    public class TestLog4NetMiddleware : IResolveMiddleware
    {
        // This phase runs just before Activation, is the recommended point at which the resolve parameters should be replaced if needed.
        public PipelinePhase Phase => PipelinePhase.ParameterSelection;

        public void Execute(ResolveRequestContext context, Action<ResolveRequestContext> next)
        {
            // Add our parameters.
            context.ChangeParameters(context.Parameters.Union(
                new[]
                {
                    new ResolvedParameter(
                        (p, i) => p.ParameterType == typeof(ILog),
                        (p, i) => LogManager.GetLogger(p.Member.DeclaringType)
                    ),
                }));

            // Continue the resolve.
            next(context);

            // Has an instance been activated?
            if (context.NewInstanceActivated)
            {
                var instanceType = context.Instance.GetType();

                // Get all the injectable properties to set.
                // If you wanted to ensure the properties were only UNSET properties,
                // here's where you'd do it.
                var properties = instanceType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.PropertyType == typeof(ILog) && p.CanWrite && p.GetIndexParameters().Length == 0);

                // Set the properties located.
                foreach (var propToSet in properties)
                {
                    propToSet.SetValue(context.Instance, LogManager.GetLogger(instanceType), null);
                }
            }
        }
    }

    [TestClass]
    public class UnitTestLog4Net
    {
        public TestContext TestContext { get; set; }

        // Create your builder.
        private static readonly ContainerBuilder container = new ContainerBuilder();

        //private static IContainer builder;
        private static Autofac.IContainer builder;
        protected ILifetimeScope scope;

        //
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext _tc)
        {
            container.RegisterType<TestClass>();
            container.RegisterModule(new TestMiddlewareModule(new TestLog4NetMiddleware()));
            //container.RegisterServiceMiddleware<TestLog4NetMiddleware>(new TestLog4NetMiddleware())
            //.AsImplementedInterfaces()
            ;

            builder = container.Build();
            //BasicConfigurator.Configure();
            //XmlConfigurator.Configure(new System.IO.FileInfo(tc.FullyQualifiedTestClassName));
            //XmlConfigurator.ConfigureAndWatch()
        }

        [ClassInitialize(InheritanceBehavior.BeforeEachDerivedClass)]
        public static void ClassInit(TestContext _tc)
        {
            log4net.GlobalContext.Properties["name"] = Path.Combine(_tc.TestRunDirectory, _tc.TestName);
        }


        [TestMethod]
        public void TestMethodLog4Net1()
        {
            log4net.LogicalThreadContext.Properties["name"] = Path.Combine(TestContext.TestRunDirectory, TestContext.TestName);

            scope = builder.BeginLifetimeScope();
            var testClass = scope.Resolve<TestClass>();

            testClass.Info("INFO1");
            testClass.Warn("WARN1");
            testClass.Debug("DEBUG1");
            testClass.Error("ERROR1");
            testClass.Fatal("FATAL1");
        }
    }
}
