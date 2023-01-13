using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.ManagersAssistant;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{
  partial class ManagersAssistantServerHandlers
  {

    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      Functions.ManagersAssistant.RemoveAssistantsFromRoleUsersWithAssignmentCompletionRights(_obj, null);
    }
    
    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      if (!_obj.State.IsCopied)
      {
        _obj.PreparesResolution = false;
        _obj.PreparesAssignmentCompletion = false;
        _obj.IsAssistant = true;
        _obj.SendActionItems = true;
      }
    }
    
    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      Functions.ManagersAssistant.UpdateRoleUsersWithAssignmentCompletionRights(_obj);
      
      if (_obj.Status == CoreEntities.DatabookEntry.Status.Closed)
        return;
      
      if (_obj.Manager == null || _obj.Assistant == null)
        return;
      
      Functions.ManagersAssistant.ValidateManagersAssistants(_obj, e);
    }
  }
}