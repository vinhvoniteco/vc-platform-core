﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Modularity.Exceptions;
using VirtoCommerce.Platform.Modules.External;

namespace VirtoCommerce.Platform.Modules
{
    public class ExternalModuleCatalog : ModuleCatalog, IExternalModuleCatalog
    {
        private readonly IEnumerable<ModuleInfo> _installedModules;
        private readonly ExternalModuleCatalogOptions _options;
        private readonly IExternalModulesClient _externalClient;
        private readonly ILogger<ExternalModuleCatalog> _logger;

        private static readonly object _lockObject = new object();

        public ExternalModuleCatalog(ILocalModuleCatalog otherCatalog, IExternalModulesClient externalClient, IOptions<ExternalModuleCatalogOptions> options, ILogger<ExternalModuleCatalog> logger)
        {
            _externalClient = externalClient;
            _installedModules = otherCatalog.Modules;
            _options = options.Value;
            _logger = logger;
        }

        #region ModuleCatalog overrides      

        protected override void InnerLoad()
        {
            lock (_lockObject)
            {
                var externalModuleInfos = LoadModulesManifests(_options.ModulesManifestUrl);
                foreach (var externalModuleInfo in externalModuleInfos)
                {
                    if (!Modules.OfType<ManifestModuleInfo>().Contains(externalModuleInfo))
                    {
                        var alreadyInstalledModule = _installedModules.OfType<ManifestModuleInfo>().FirstOrDefault(x => x.Equals(externalModuleInfo));
                        if (alreadyInstalledModule != null)
                        {
                            externalModuleInfo.IsInstalled = alreadyInstalledModule.IsInstalled;
                            externalModuleInfo.Errors = alreadyInstalledModule.Errors;
                        }
                        externalModuleInfo.InitializationMode = InitializationMode.OnDemand;
                        AddModule(externalModuleInfo);
                    }
                }

                // Add already installed module not presenting in external modules list
                foreach (var installedModuleNotFoundInExternal in _installedModules.Except(Modules))
                {
                    AddModule(installedModuleNotFoundInExternal);
                }
            }
        }

        /// <summary>
        /// Return all dependencies with best compatible version
        /// </summary>
        /// <param name="moduleInfo"></param>
        /// <returns></returns>
        protected override IEnumerable<ModuleInfo> GetDependentModulesInner(ModuleInfo moduleInfo)
        {
            var manifestModule = moduleInfo as ManifestModuleInfo;
            if (manifestModule == null)
            {
                throw new ModularityException("moduleInfo is not ManifestModuleInfo type");
            }

            var result = new List<ModuleInfo>();

            // Get all dependency modules with all versions
            var dependecies = base.GetDependentModulesInner(moduleInfo).OfType<ManifestModuleInfo>();

            foreach (var dependencyGroup in dependecies.GroupBy(x => x.Id))
            {
                var dependency = manifestModule.Dependencies.First(x => x.Id == dependencyGroup.Key);
                var allCompatibleDependencies = dependencyGroup.Where(x => dependency.Version.IsCompatibleWithBySemVer(x.Version)).OrderByDescending(x => x.Version);
                var latestCompatibleDependency = allCompatibleDependencies.FirstOrDefault(x => x.IsInstalled);

                // If dependency is not installed, find latest compatible version
                if (latestCompatibleDependency == null)
                {
                    latestCompatibleDependency = allCompatibleDependencies.FirstOrDefault();
                }

                if (latestCompatibleDependency != null)
                {
                    result.Add(latestCompatibleDependency);
                }
            }

            return result;
        }

        protected override void ValidateUniqueModules()
        {
            var modules = Modules.OfType<ManifestModuleInfo>().ToList();

            var duplicateModule = modules.FirstOrDefault(x => modules.Count(y => y.Equals(x)) > 1);

            if (duplicateModule != null)
            {
                throw new DuplicateModuleException(duplicateModule.ToString());
            }
        }

        #endregion

        private IEnumerable<ManifestModuleInfo> LoadModulesManifests(Uri manifestUrl)
        {
            var result = new List<ManifestModuleInfo>();

            try
            {
                _logger.LogDebug("Download module manifests from " + manifestUrl);

                using (var stream = _externalClient.OpenRead(manifestUrl))
                {
                    var manifests = stream.DeserializeJson<List<ModuleManifest>>();
                    if (!manifests.IsNullOrEmpty())
                    {
                        result.AddRange(manifests.Select(manifest => new ManifestModuleInfo(manifest)));
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }
            return result;
        }

    }
}
