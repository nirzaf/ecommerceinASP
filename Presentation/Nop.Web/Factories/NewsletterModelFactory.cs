﻿using Nop.Core.Domain.Customers;
using Nop.Services.Localization;
using Nop.Web.Models.Newsletter;

namespace Nop.Web.Factories
{
    /// <summary>
    /// Represents the newsletter model factory
    /// </summary>
    public partial class NewsletterModelFactory : INewsletterModelFactory
    {
        #region Fields

        private readonly CustomerSettings _customerSettings;
        private readonly ILocalizationService _localizationService;

        #endregion

        #region Ctor

        public NewsletterModelFactory(CustomerSettings customerSettings,
            ILocalizationService localizationService)
        {
            _customerSettings = customerSettings;
            _localizationService = localizationService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Prepare the newsletter box model
        /// </summary>
        /// <returns>Newsletter box model</returns>
        public virtual NewsletterBoxModel PrepareNewsletterBoxModel()
        {
            var model = new NewsletterBoxModel()
            {
                AllowToUnsubscribe = _customerSettings.NewsletterBlockAllowToUnsubscribe
            };
            return model;
        }

        /// <summary>
        /// Prepare the subscription activation model
        /// </summary>
        /// <param name="active">Whether the subscription has been activated</param>
        /// <returns>Subscription activation model</returns>
        public virtual SubscriptionActivationModel PrepareSubscriptionActivationModel(bool active)
        {
            var model = new SubscriptionActivationModel
            {
                Result = active
                ? _localizationService.GetResource("Newsletter.ResultActivated")
                : _localizationService.GetResource("Newsletter.ResultDeactivated")
            };

            return model;
        }

        #endregion
    }
}
