﻿using System.Collections.Generic;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Tax;
using Nop.Services.Plugins;

namespace Nop.Services.Tax
{
    /// <summary>
    /// Represents a tax plugin manager implementation
    /// </summary>
    public partial class TaxPluginManager : PluginManager<ITaxProvider>, ITaxPluginManager
    {
        #region Fields

        private readonly TaxSettings _taxSettings;

        #endregion

        #region Ctor

        public TaxPluginManager(IPluginService pluginService,
            TaxSettings taxSettings) : base(pluginService)
        {
            _taxSettings = taxSettings;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Load primary active tax provider
        /// </summary>
        /// <param name="customer">Filter by customer; pass null to load all plugins</param>
        /// <param name="storeId">Filter by store; pass 0 to load all plugins</param>
        /// <returns>Tax provider</returns>
        public virtual ITaxProvider LoadPrimaryPlugin(Customer customer = null, int storeId = 0)
        {
            return LoadPrimaryPlugin(_taxSettings.ActiveTaxProviderSystemName, customer, storeId);
        }

        /// <summary>
        /// Check whether the passed tax provider is active
        /// </summary>
        /// <param name="taxProvider">Tax provider to check</param>
        /// <returns>Result</returns>
        public virtual bool IsPluginActive(ITaxProvider taxProvider)
        {
            return IsPluginActive(taxProvider, new List<string> { _taxSettings.ActiveTaxProviderSystemName });
        }

        /// <summary>
        /// Check whether the tax provider with the passed system name is active
        /// </summary>
        /// <param name="systemName">System name of tax provider to check</param>
        /// <param name="customer">Filter by customer; pass null to load all plugins</param>
        /// <param name="storeId">Filter by store; pass 0 to load all plugins</param>
        /// <returns>Result</returns>
        public virtual bool IsPluginActive(string systemName, Customer customer = null, int storeId = 0)
        {
            var taxProvider = LoadPluginBySystemName(systemName, customer, storeId);
            return IsPluginActive(taxProvider);
        }

        #endregion
    }
}