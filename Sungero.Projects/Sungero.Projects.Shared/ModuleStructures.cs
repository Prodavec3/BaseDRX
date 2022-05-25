using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Projects.Structures.Module
{

  /// <summary>
  /// Права участника проекта на проектные документы.
  /// </summary>
  partial class RecipientRights
  {
    public IRecipient Recipient { get; set; }
    
    public string AccessRights { get; set; }
  }

}