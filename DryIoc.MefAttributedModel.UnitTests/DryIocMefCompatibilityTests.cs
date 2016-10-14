﻿using System.Linq;
using DryIoc.MefAttributedModel.UnitTests.CUT;
using NUnit.Framework;

namespace DryIoc.MefAttributedModel.UnitTests
{
    [TestFixture]
    public class DryIocMefCompatibilityTests
    {
        private IContainer Container => CreateContainer();

        private static IContainer CreateContainer()
        {
            var container = new Container().WithMef();

            container.RegisterExports(new[] { typeof(ILogTableManager).GetAssembly() });
            return container;
        }

        [Test]
        public void DryIoc_supports_importing_static_factory_method()
        {
            // LogTableManagerConsumer creates ILogTableManager via unnamed factory method with parameters
            var export = Container.Resolve<LogTableManagerConsumer1>();

            Assert.IsNotNull(export);
            Assert.IsNotNull(export.LogTableManager);
            Assert.AreEqual("SCHEMA1.LOG_ENTRIES", export.LogTableManager.TableName);
        }

        [Test]
        public void DryIoc_supports_importing_named_static_factory_method()
        {
            // LogTableManagerConsumer creates ILogTableManager via named factory method with parameters
            var export = Container.Resolve<LogTableManagerConsumer2>();

            Assert.IsNotNull(export);
            Assert.IsNotNull(export.LogTableManager);
            Assert.AreEqual("SCHEMA2.LOG_ENTRIES", export.LogTableManager.TableName);
        }

        [Test]
        public void DryIoc_supports_exporting_instance_member_of_not_exported_type()
        {
            var container = new Container().WithMefAttributedModel();

            container.RegisterExports(typeof(Provider));
            var abc = container.Resolve<Abc>();

            Assert.IsNotNull(abc);
            Assert.IsNull(container.Resolve<Provider>(IfUnresolved.ReturnDefault));
        }

        [Test, Ignore("fails")]
        public void DryIoc_supports_named_value_imports_and_exports()
        {
            // SettingImportHelper gathers all exported string settings from the catalog
            var importer = Container.Resolve<SettingImportHelper>();

            Assert.IsNotNull(importer);
            Assert.IsNotNull(importer.ImportedValues);
            Assert.AreEqual(4, importer.ImportedValues.Length);

            Assert.IsTrue(importer.ImportedValues.Contains("Constants.ExportedValue"));
            Assert.IsTrue(importer.ImportedValues.Contains("SettingProvider1.ExportedValue"));
            Assert.IsTrue(importer.ImportedValues.Contains("SettingProvider2.ExportedValue"));
            Assert.IsTrue(importer.ImportedValues.Contains("SettingProvider3.ExportedValue"));
        }

        [Test]
        public void DryIoc_supports_multiple_contract_names_on_same_service_type()
        {
            var importer = Container.Resolve<ImportAllProtocolVersions>();

            Assert.IsNotNull(importer);
            Assert.IsNotNull(importer.Protocols);
            Assert.AreEqual(4, importer.Protocols.Length);

            Assert.IsTrue(importer.Protocols.Any(v => v.Version == "1.0"));
            Assert.IsTrue(importer.Protocols.Any(v => v.Version == "2.0"));
            Assert.IsTrue(importer.Protocols.Any(v => v.Version == "3.0"));
            Assert.IsTrue(importer.Protocols.Any(v => v.Version == "4.0"));
        }

        [Test, Ignore("fails")]
        public void DryIoc_supports_importing_service_as_untyped_property()
        {
            var importer = Container.Resolve<ImportUntypedService>();

            Assert.IsNotNull(importer);
            Assert.IsNotNull(importer);
            Assert.IsNotNull(importer.UntypedService);
            Assert.AreEqual(typeof(UntypedService), importer.UntypedService.GetType());
        }

        [Test, Ignore("fails")]
        public void DryIoc_supports_importing_services_as_untyped_array()
        {
            var importer = Container.Resolve<ImportManyUntypedServices>();

            Assert.IsNotNull(importer);
            Assert.IsNotNull(importer);
            Assert.IsNotNull(importer.UntypedServices);
            Assert.AreEqual(1, importer.UntypedServices.Length);
        }

        [Test]
        public void DryIoc_chooses_the_default_constructor_if_no_constructors_are_marked_with_ImportingConstructorAttribute()
        {
            var service = Container.Resolve<IServiceWithTwoConstructors>();

            Assert.IsNotNull(service);
            Assert.IsTrue(service.DefaultConstructorIsUsed);
        }

        [Test]
        public void DryIoc_supports_ExportFactory_for_non_shared_parts()
        {
            var container = Container;
            var service = container.Resolve<UsesExportFactoryOfNonSharedService>();

            Assert.IsNotNull(service);
            Assert.IsNotNull(service.Factory);

            NonSharedDependency nonSharedDependency;
            using (var scope = container.OpenScope())
            {
                nonSharedDependency = scope.Resolve<NonSharedDependency>();
                Assert.IsFalse(nonSharedDependency.IsDisposed);
            }

            Assert.IsTrue(nonSharedDependency.IsDisposed);

            NonSharedService nonSharedService;
            using (var export = service.Factory.CreateExport())
            {
                nonSharedService = export.Value;
                Assert.IsNotNull(nonSharedService);
                Assert.IsFalse(nonSharedService.IsDisposed);

                Assert.IsNotNull(nonSharedService.NonSharedDependency);
                Assert.IsFalse(nonSharedService.NonSharedDependency.IsDisposed);

                Assert.IsNotNull(nonSharedService.SharedDependency);
                Assert.IsFalse(nonSharedService.SharedDependency.IsDisposed);
            }

            Assert.IsTrue(nonSharedService.IsDisposed);
            Assert.IsTrue(nonSharedService.NonSharedDependency.IsDisposed);
            Assert.IsFalse(nonSharedService.SharedDependency.IsDisposed);
        }

        [Test]
        public void DryIoc_supports_ExportFactory_for_shared_parts()
        {
            var container = Container;
            var service = container.Resolve<UsesExportFactoryOfSharedService>();

            Assert.IsNotNull(service);
            Assert.IsNotNull(service.Factory);

            SharedService sharedService;
            using (var export = service.Factory.CreateExport())
            {
                sharedService = export.Value;
                Assert.IsNotNull(sharedService);
                Assert.IsFalse(sharedService.IsDisposed);
            }

            Assert.IsFalse(sharedService.IsDisposed);
            Assert.IsFalse(sharedService.NonSharedDependency.IsDisposed);
            Assert.IsFalse(sharedService.SharedDependency.IsDisposed);

            container.Dispose();
            Assert.IsTrue(sharedService.IsDisposed);
            Assert.IsTrue(sharedService.NonSharedDependency.IsDisposed);
            Assert.IsTrue(sharedService.SharedDependency.IsDisposed);
        }

        [Test]
        public void DryIoc_can_import_many_lazy_services_with_metadata()
        {
            var service = Container.Resolve<ImportLazyNamedServices>();

            Assert.IsNotNull(service);
            Assert.IsNotNull(service.LazyNamedServices);

            var services = service.LazyNamedServices.OrderBy(l => l.Metadata.Name).ToArray();
            Assert.AreEqual(2, services.Length);
            Assert.AreEqual("One", services[0].Metadata.Name);
            Assert.AreEqual("Two", services[1].Metadata.Name);
            Assert.IsNotNull(services[0].Value);
            Assert.IsNotNull(services[1].Value);
        }
    }
}