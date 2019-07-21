﻿using System;
using System.Linq;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Framework.Factories;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Web.Areas.Admin.Factories
{
    /// <summary>
    /// Represents the product attribute model factory implementation
    /// </summary>
    public partial class ProductAttributeModelFactory : IProductAttributeModelFactory
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly ILocalizedModelFactory _localizedModelFactory;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IProductService _productService;

        #endregion

        #region Ctor

        public ProductAttributeModelFactory(ILocalizationService localizationService,
            ILocalizedModelFactory localizedModelFactory,
            IProductAttributeService productAttributeService,
            IProductService productService)
        {
            _localizationService = localizationService;
            _localizedModelFactory = localizedModelFactory;
            _productAttributeService = productAttributeService;
            _productService = productService;
        }

        #endregion

        #region Utilities
        
        /// <summary>
        /// Prepare predefined product attribute value search model
        /// </summary>
        /// <param name="searchModel">Predefined product attribute value search model</param>
        /// <param name="productAttribute">Product attribute</param>
        /// <returns>Predefined product attribute value search model</returns>
        protected virtual PredefinedProductAttributeValueSearchModel PreparePredefinedProductAttributeValueSearchModel(
            PredefinedProductAttributeValueSearchModel searchModel, ProductAttribute productAttribute)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            if (productAttribute == null)
                throw new ArgumentNullException(nameof(productAttribute));

            searchModel.ProductAttributeId = productAttribute.Id;

            //prepare page parameters
            searchModel.SetGridPageSize();

            return searchModel;
        }

        /// <summary>
        /// Prepare search model of products that use the product attribute
        /// </summary>
        /// <param name="searchModel">Search model of products that use the product attribute</param>
        /// <param name="productAttribute">Product attribute</param>
        /// <returns>Search model of products that use the product attribute</returns>
        protected virtual ProductAttributeProductSearchModel PrepareProductAttributeProductSearchModel(ProductAttributeProductSearchModel searchModel,
            ProductAttribute productAttribute)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            if (productAttribute == null)
                throw new ArgumentNullException(nameof(productAttribute));

            searchModel.ProductAttributeId = productAttribute.Id;

            //prepare page parameters
            searchModel.SetGridPageSize();

            return searchModel;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Prepare product attribute search model
        /// </summary>
        /// <param name="searchModel">Product attribute search model</param>
        /// <returns>Product attribute search model</returns>
        public virtual ProductAttributeSearchModel PrepareProductAttributeSearchModel(ProductAttributeSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            return searchModel;
        }

        /// <summary>
        /// Prepare paged product attribute list model
        /// </summary>
        /// <param name="searchModel">Product attribute search model</param>
        /// <returns>Product attribute list model</returns>
        public virtual ProductAttributeListModel PrepareProductAttributeListModel(ProductAttributeSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get product attributes
            var productAttributes = _productAttributeService
                .GetAllProductAttributes(pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

            //prepare list model
            var model = new ProductAttributeListModel().PrepareToGrid(searchModel, productAttributes, () =>
            {
                //fill in model values from the entity
                return productAttributes.Select(attribute => attribute.ToModel<ProductAttributeModel>());
                
            });

            return model;
        }

        /// <summary>
        /// Prepare product attribute model
        /// </summary>
        /// <param name="model">Product attribute model</param>
        /// <param name="productAttribute">Product attribute</param>
        /// <param name="excludeProperties">Whether to exclude populating of some properties of model</param>
        /// <returns>Product attribute model</returns>
        public virtual ProductAttributeModel PrepareProductAttributeModel(ProductAttributeModel model,
            ProductAttribute productAttribute, bool excludeProperties = false)
        {
            Action<ProductAttributeLocalizedModel, int> localizedModelConfiguration = null;

            if (productAttribute != null)
            {
                //fill in model values from the entity
                model = model ?? productAttribute.ToModel<ProductAttributeModel>();

                //prepare nested search models
                PreparePredefinedProductAttributeValueSearchModel(model.PredefinedProductAttributeValueSearchModel, productAttribute);
                PrepareProductAttributeProductSearchModel(model.ProductAttributeProductSearchModel, productAttribute);

                //define localized model configuration action
                localizedModelConfiguration = (locale, languageId) =>
                {
                    locale.Name = _localizationService.GetLocalized(productAttribute, entity => entity.Name, languageId, false, false);
                    locale.Description = _localizationService.GetLocalized(productAttribute, entity => entity.Description, languageId, false, false);
                };
            }

            //prepare localized models
            if (!excludeProperties)
                model.Locales = _localizedModelFactory.PrepareLocalizedModels(localizedModelConfiguration);

            return model;
        }

        /// <summary>
        /// Prepare paged predefined product attribute value list model
        /// </summary>
        /// <param name="searchModel">Predefined product attribute value search model</param>
        /// <param name="productAttribute">Product attribute</param>
        /// <returns>Predefined product attribute value list model</returns>
        public virtual PredefinedProductAttributeValueListModel PreparePredefinedProductAttributeValueListModel(
            PredefinedProductAttributeValueSearchModel searchModel, ProductAttribute productAttribute)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            if (productAttribute == null)
                throw new ArgumentNullException(nameof(productAttribute));

            //get predefined product attribute values
            var values = _productAttributeService.GetPredefinedProductAttributeValues(productAttribute.Id).ToPagedList(searchModel);

            //prepare list model
            var model = new PredefinedProductAttributeValueListModel().PrepareToGrid(searchModel, values, () =>
            {
                return values.Select(value =>
                {
                    //fill in model values from the entity
                    var predefinedProductAttributeValueModel = value.ToModel<PredefinedProductAttributeValueModel>();

                    //fill in additional values (not existing in the entity)
                    predefinedProductAttributeValueModel.WeightAdjustmentStr = value.WeightAdjustment.ToString("G29");
                    predefinedProductAttributeValueModel.PriceAdjustmentStr = value.PriceAdjustment
                        .ToString("G29") + (value.PriceAdjustmentUsePercentage ? " %" : string.Empty);

                    return predefinedProductAttributeValueModel;
                });
            });

            return model;
        }

        /// <summary>
        /// Prepare predefined product attribute value model
        /// </summary>
        /// <param name="model">Predefined product attribute value model</param>
        /// <param name="productAttribute">Product attribute</param>
        /// <param name="productAttributeValue">Predefined product attribute value</param>
        /// <param name="excludeProperties">Whether to exclude populating of some properties of model</param>
        /// <returns>Predefined product attribute value model</returns>
        public virtual PredefinedProductAttributeValueModel PreparePredefinedProductAttributeValueModel(PredefinedProductAttributeValueModel model,
            ProductAttribute productAttribute, PredefinedProductAttributeValue productAttributeValue, bool excludeProperties = false)
        {
            if (productAttribute == null)
                throw new ArgumentNullException(nameof(productAttribute));

            Action<PredefinedProductAttributeValueLocalizedModel, int> localizedModelConfiguration = null;

            if (productAttributeValue != null)
            {
                //fill in model values from the entity
                if (model == null) 
                {
                    model = productAttributeValue.ToModel<PredefinedProductAttributeValueModel>();
                }

                //define localized model configuration action
                localizedModelConfiguration = (locale, languageId) =>
                {
                    locale.Name = _localizationService.GetLocalized(productAttributeValue, entity => entity.Name, languageId, false, false);
                };
            }

            model.ProductAttributeId = productAttribute.Id;

            //prepare localized models
            if (!excludeProperties)
                model.Locales = _localizedModelFactory.PrepareLocalizedModels(localizedModelConfiguration);

            return model;
        }

        /// <summary>
        /// Prepare paged list model of products that use the product attribute
        /// </summary>
        /// <param name="searchModel">Search model of products that use the product attribute</param>
        /// <param name="productAttribute">Product attribute</param>
        /// <returns>List model of products that use the product attribute</returns>
        public virtual ProductAttributeProductListModel PrepareProductAttributeProductListModel(ProductAttributeProductSearchModel searchModel,
            ProductAttribute productAttribute)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            if (productAttribute == null)
                throw new ArgumentNullException(nameof(productAttribute));

            //get products
            var products = _productService.GetProductsByProductAtributeId(productAttributeId: productAttribute.Id,
                pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

            //prepare list model
            var model = new ProductAttributeProductListModel().PrepareToGrid(searchModel, products, () =>
            {
                //fill in model values from the entity
                return products.Select(product =>
                {
                    var productAttributeProductModel = product.ToModel<ProductAttributeProductModel>();
                    productAttributeProductModel.ProductName = product.Name;
                    return productAttributeProductModel;
                });
            });

            return model;
        }

        #endregion
    }
}