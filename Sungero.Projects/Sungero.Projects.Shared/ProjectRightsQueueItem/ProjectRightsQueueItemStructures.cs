using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Projects.Structures.ProjectRightsQueueItem
{
  partial class ProxyQueueItem
  {
    public int Id { get; set; }
    
    public Guid Discriminator { get; set; }
    
    public int ProjectId_Project_Sungero { get; set; }
    
    public int? FolderId_Project_Sungero { get; set; }
  }

}