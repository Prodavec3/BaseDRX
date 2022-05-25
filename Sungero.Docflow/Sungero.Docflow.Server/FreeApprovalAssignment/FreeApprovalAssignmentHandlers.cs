using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalAssignment;

namespace Sungero.Docflow
{
  partial class FreeApprovalAssignmentServerHandlers
  {

    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      if (_obj.State.IsInserted)
      {
        // Создание нового задания может изменить срок задачи.
        if (_obj.Task.MaxDeadline.HasValue && Functions.Module.CheckDeadline(_obj.Deadline, _obj.Task.MaxDeadline))
          _obj.Task.MaxDeadline = _obj.Deadline;
      }
    }

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      if (_obj.Task.GetStartedSchemeVersion() > LayerSchemeVersions.V2)
      {
        if (!Functions.Module.CheckDeadline(_obj.Addressee ?? Users.Current, _obj.AddresseeDeadline, Calendar.Now))
          e.AddError(_obj.Info.Properties.AddresseeDeadline, FreeApprovalTasks.Resources.ImpossibleSpecifyDeadlineLessThanToday);
        
        if (!Functions.FreeApprovalAssignment.CanForwardTo(_obj, _obj.Addressee))
          e.AddError(_obj.Info.Properties.Addressee, FreeApprovalAssignments.Resources.ApproverAlreadyExistsFormat(_obj.Addressee.Person.ShortName));
      }
      
      if (_obj.Result == Result.Approved)
        e.Result = Docflow.FreeApprovalTasks.Resources.Approved;
      else if (_obj.Result == Result.Forward)
        e.Result = FreeApprovalTasks.Resources.ForwardedFormat(Company.PublicFunctions.Employee.GetShortName(_obj.Addressee, DeclensionCase.Dative, true));
      else
        e.Result = Docflow.FreeApprovalTasks.Resources.ForRework;
    }
  }

}