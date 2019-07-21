﻿using System.Collections.Generic;
using Nop.Web.Framework.Models;

namespace Nop.Web.Models.Boards
{
    public partial class BoardsIndexModel : BaseNopModel
    {
        public BoardsIndexModel()
        {
            ForumGroups = new List<ForumGroupModel>();
        }
        
        public IList<ForumGroupModel> ForumGroups { get; set; }
    }
}