using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalAssignment;

namespace Sungero.Docflow
{
  partial class FreeApprovalAssignmentClientHandlers
  {

    public virtual void AddresseeValueInput(Sungero.Docflow.Client.FreeApprovalAssignmentAddresseeValueInputEventArgs e)
    {
      var warnMessage = Docflow.Functions.Module.CheckDeadlineByWorkCalendar(e.NewValue ?? Users.Current, _obj.AddresseeDeadline);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
      
      if (!Docflow.PublicFunctions.Module.CheckDeadline(e.NewValue ?? Users.Current, _obj.AddresseeDeadline, Calendar.Now))
        e.AddError(FreeApprovalTasks.Resources.ImpossibleSpecifyDeadlineLessThanToday);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      // Срок для переадресации нужен, если у самого задания есть срок, плюс указан сотрудник, которому переадресуют.
      _obj.State.Properties.AddresseeDeadline.IsEnabled = _obj.Addressee != null;
      _obj.State.Properties.AddresseeDeadline.IsRequired = _obj.Deadline.HasValue && _obj.Addressee != null;
      
      var canReadDocument = Functions.FreeApprovalTask.HasDocumentAndCanRead(FreeApprovalTasks.As(_obj.Task));
      var schemeVersion = _obj.Task.GetStartedSchemeVersion();
      var oldVersion = schemeVersion == LayerSchemeVersions.V1 || schemeVersion == LayerSchemeVersions.V2;
      _obj.State.Properties.Addressee.IsVisible = !oldVersion && canReadDocument;
      _obj.State.Properties.AddresseeDeadline.IsVisible = !oldVersion && canReadDocument;
      
      if (!canReadDocument)
        e.AddError(Docflow.Resources.NoRightsToDocument);
    }

    public virtual void AddresseeDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      var warnMessage = Docflow.Functions.Module.CheckDeadlineByWorkCalendar(_obj.Addressee ?? Users.Current, e.NewValue);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
      
      if (!Docflow.PublicFunctions.Module.CheckDeadline(_obj.Addressee ?? Users.Current, e.NewValue, Calendar.Now))
        e.AddError(FreeApprovalTasks.Resources.ImpossibleSpecifyDeadlineLessThanToday);
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      var schemeVersion = _obj.Task.GetStartedSchemeVersion();
      if (schemeVersion == LayerSchemeVersions.V1 || schemeVersion == LayerSchemeVersions.V2)
      {
        e.HideAction(_obj.Info.Actions.Forward);
        e.HideAction(_obj.Info.Actions.AddApprover);
      }
    }
  }

}