﻿using System.Collections.Generic;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Models.Catalog
{
    /// <summary>
    /// Represents a product attribute model
    /// </summary>
    public partial class ProductAttributeModel : BaseNopEntityModel, ILocalizedModel<ProductAttributeLocalizedModel>
    {
        #region Ctor

        public ProductAttributeModel()
        {
            Locales = new List<ProductAttributeLocalizedModel>();
            PredefinedProductAttributeValueSearchModel = new PredefinedProductAttributeValueSearchModel();
            ProductAttributeProductSearchModel = new ProductAttributeProductSearchModel();
        }

        #endregion

        #region Properties

        [NopResourceDisplayName("Admin.Catalog.Attributes.ProductAttributes.Fields.Name")]
        public string Name { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Attributes.ProductAttributes.Fields.Description")]
        public string Description {get;set;}

        public IList<ProductAttributeLocalizedModel> Locales { get; set; }

        public PredefinedProductAttributeValueSearchModel PredefinedProductAttributeValueSearchModel { get; set; }

        public ProductAttributeProductSearchModel ProductAttributeProductSearchModel { get; set; }

        #endregion
    }

    public partial class ProductAttributeLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Attributes.ProductAttributes.Fields.Name")]
        public string Name { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Attributes.ProductAttributes.Fields.Description")]
        public string Description {get;set;}
    }
}