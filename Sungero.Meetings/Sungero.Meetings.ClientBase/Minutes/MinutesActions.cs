using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Meetings.Minutes;

namespace Sungero.Meetings.Client
{
  partial class MinutesActions
  {
    public virtual void CreateActionItems(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Docflow.PublicFunctions.OfficialDocument.CreateActionItemsFromDocumentDialog(_obj, e);
    }

    public virtual bool CanCreateActionItems(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.HasVersions && !_obj.State.IsInserted;
    }
  }
}