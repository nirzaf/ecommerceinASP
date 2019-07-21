﻿using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.Reports
{
    /// <summary>
    /// Represents a registered customers report model
    /// </summary>
    public partial class RegisteredCustomersReportModel : BaseNopModel
    {
        #region Properties

        [NopResourceDisplayName("Admin.Reports.Customers.RegisteredCustomers.Fields.Period")]
        public string Period { get; set; }

        [NopResourceDisplayName("Admin.Reports.Customers.RegisteredCustomers.Fields.Customers")]
        public int Customers { get; set; }

        #endregion
    }
}