﻿using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.Cms
{
    public partial class RenderWidgetModel : BaseNopModel
    {
        public string WidgetViewComponentName { get; set; }
        public object WidgetViewComponentArguments { get; set; }
    }
}