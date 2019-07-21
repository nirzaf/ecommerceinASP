﻿using System.Collections.Generic;
using Nop.Web.Framework.Models;
using Nop.Web.Models.Common;

namespace Nop.Web.Models.Profile
{
    public partial class ProfilePostsModel : BaseNopModel
    {
        public IList<PostsModel> Posts { get; set; }
        public PagerModel PagerModel { get; set; }
    }
}