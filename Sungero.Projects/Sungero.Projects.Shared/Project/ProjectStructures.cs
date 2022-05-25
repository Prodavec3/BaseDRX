using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Projects.Structures.Project
{
  partial class ProjectMemberRights
  {
    public IRecipient Recipient { get; set; }
           
    public string ProjectRightsType { get; set; }
    
    public string FoldersRightsType { get; set; }
  }
  
  [Public]
  partial class ProjectFolders
  {
    public int ProjectId { get; set; }
    
    public IFolder Folder { get; set; }
  }  
}