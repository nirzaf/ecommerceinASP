﻿using System;
using System.Linq;
using Nop.Services.Authentication.External;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.ExternalAuthentication;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Web.Areas.Admin.Factories
{
    /// <summary>
    /// Represents the external authentication method model factory implementation
    /// </summary>
    public partial class ExternalAuthenticationMethodModelFactory : IExternalAuthenticationMethodModelFactory
    {
        #region Fields

        private readonly IAuthenticationPluginManager _authenticationPluginManager;

        #endregion

        #region Ctor

        public ExternalAuthenticationMethodModelFactory(IAuthenticationPluginManager authenticationPluginManager)
        {
            _authenticationPluginManager = authenticationPluginManager;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Prepare external authentication method search model
        /// </summary>
        /// <param name="searchModel">External authentication method search model</param>
        /// <returns>External authentication method search model</returns>
        public virtual ExternalAuthenticationMethodSearchModel PrepareExternalAuthenticationMethodSearchModel(
            ExternalAuthenticationMethodSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            return searchModel;
        }

        /// <summary>
        /// Prepare paged external authentication method list model
        /// </summary>
        /// <param name="searchModel">External authentication method search model</param>
        /// <returns>External authentication method list model</returns>
        public virtual ExternalAuthenticationMethodListModel PrepareExternalAuthenticationMethodListModel(
            ExternalAuthenticationMethodSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get external authentication methods
            var externalAuthenticationMethods = _authenticationPluginManager.LoadAllPlugins().ToPagedList(searchModel);

            //prepare grid model
            var model = new ExternalAuthenticationMethodListModel().PrepareToGrid(searchModel, externalAuthenticationMethods, () =>
            {
                //fill in model values from the entity
                return externalAuthenticationMethods.Select(method =>
                {
                    //fill in model values from the entity
                    var externalAuthenticationMethodModel = method.ToPluginModel<ExternalAuthenticationMethodModel>();

                    //fill in additional values (not existing in the entity)
                    externalAuthenticationMethodModel.IsActive = _authenticationPluginManager.IsPluginActive(method);
                    externalAuthenticationMethodModel.ConfigurationUrl = method.GetConfigurationPageUrl();

                    return externalAuthenticationMethodModel;
                });
            });

            return model;
        }

        #endregion
    }
}