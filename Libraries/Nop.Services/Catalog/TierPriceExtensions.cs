﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Infrastructure;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Tier price extensions
    /// </summary>
    public static class TierPriceExtensions
    {
        /// <summary>
        /// Filter tier prices by a store
        /// </summary>
        /// <param name="source">Tier prices</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>Filtered tier prices</returns>
        public static IEnumerable<TierPrice> FilterByStore(this IEnumerable<TierPrice> source, int storeId)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Where(tierPrice => tierPrice.StoreId == 0 || tierPrice.StoreId == storeId);
        }

        /// <summary>
        /// Filter tier prices for a customer
        /// </summary>
        /// <param name="source">Tier prices</param>
        /// <param name="customer">Customer</param>
        /// <returns>Filtered tier prices</returns>
        public static IEnumerable<TierPrice> FilterForCustomer(this IEnumerable<TierPrice> source, Customer customer)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var catalogSettings = EngineContext.Current.Resolve<CatalogSettings>();
            if (catalogSettings.IgnoreAcl)
                return source;

            var customerRoleIds = customer.GetCustomerRoleIds();
            return source.Where(tierPrice =>
               !tierPrice.CustomerRoleId.HasValue ||
               tierPrice.CustomerRoleId.Value == 0 ||
               customerRoleIds.Contains(tierPrice.CustomerRoleId.Value));
        }

        /// <summary>
        /// Remove duplicated quantities (leave only an tier price with minimum price)
        /// </summary>
        /// <param name="source">Tier prices</param>
        /// <returns>Filtered tier prices</returns>
        public static IEnumerable<TierPrice> RemoveDuplicatedQuantities(this IEnumerable<TierPrice> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var tierPrices = source.ToList();

            //get group of tier prices with the same quantity
            var tierPricesWithDuplicates = tierPrices.GroupBy(tierPrice => tierPrice.Quantity).Where(group => group.Count() > 1);

            //get tier prices with higher prices 
            var duplicatedPrices = tierPricesWithDuplicates.SelectMany(group =>
            {
                //find minimal price for quantity
                var minTierPrice = group.Aggregate((currentMinTierPrice, nextTierPrice) =>
                    (currentMinTierPrice.Price < nextTierPrice.Price ? currentMinTierPrice : nextTierPrice));

                //and return all other with higher price
                return group.Where(tierPrice => tierPrice.Id != minTierPrice.Id);
            });

            //return tier prices without duplicates
            return tierPrices.Where(tierPrice => duplicatedPrices.All(duplicatedPrice => duplicatedPrice.Id != tierPrice.Id));
        }

        /// <summary>
        /// Filter tier prices by date
        /// </summary>
        /// <param name="source">Tier prices</param>
        /// <param name="date">Date in UTC; pass null to filter by current date</param>
        /// <returns>Filtered tier prices</returns>
        public static IEnumerable<TierPrice> FilterByDate(this IEnumerable<TierPrice> source, DateTime? date = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (!date.HasValue)
                date = DateTime.UtcNow;

            return source.Where(tierPrice =>
                (!tierPrice.StartDateTimeUtc.HasValue || tierPrice.StartDateTimeUtc.Value < date) &&
                (!tierPrice.EndDateTimeUtc.HasValue || tierPrice.EndDateTimeUtc.Value > date));
        }
    }
}
