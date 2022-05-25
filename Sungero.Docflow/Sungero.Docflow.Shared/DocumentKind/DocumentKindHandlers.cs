using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentKind;

namespace Sungero.Docflow
{
  partial class DocumentKindSharedHandlers
  {

    public virtual void ProjectsAccountingChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue == false)
        _obj.GrantRightsToProject = false;
    }

    public virtual void DocumentFlowChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
        _obj.DocumentType = null;
      
      if (e.NewValue != e.OldValue)
      {
        var actions = _obj.AvailableActions.ToList();
        foreach (var action in actions)
        {
          _obj.AvailableActions.Remove(action);
        }
      }
      
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue))
      {
        if (e.NewValue != DocumentFlow.Outgoing)
          _obj.AvailableActions.AddNew().Action = Functions.Module.GetSendAction(OfficialDocuments.Info.Actions.SendActionItem);
        
        if (e.NewValue == DocumentFlow.Incoming)
        {
          _obj.AvailableActions.AddNew().Action = Functions.Module.GetSendAction(OfficialDocuments.Info.Actions.SendForReview);
        }
        else
        {
          _obj.AvailableActions.AddNew().Action = Functions.Module.GetSendAction(OfficialDocuments.Info.Actions.SendForFreeApproval);
          _obj.AvailableActions.AddNew().Action = Functions.Module.GetSendAction(OfficialDocuments.Info.Actions.SendForApproval);
        }
      }
    }
    
    public virtual void NumberingTypeChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      var isNumerable = e.NewValue == NumberingType.Numerable;
      var isRegistrable = e.NewValue == NumberingType.Registrable;
      
      _obj.State.Properties.AutoNumbering.IsVisible = isNumerable;
      _obj.GenerateDocumentName = isNumerable || isRegistrable;
    }

    public virtual void NameChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      if (_obj.ShortName == null)
        _obj.ShortName = e.NewValue;
    }
  }
}