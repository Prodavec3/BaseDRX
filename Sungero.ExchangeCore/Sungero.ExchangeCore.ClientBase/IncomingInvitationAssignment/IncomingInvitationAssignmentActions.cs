using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.IncomingInvitationAssignment;

namespace Sungero.ExchangeCore.Client
{
  partial class IncomingInvitationAssignmentActions
  {
    public virtual void Reject(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var task = IncomingInvitationTasks.As(_obj.Task);
      var boxLine = task.Counterparty.ExchangeBoxes.FirstOrDefault(x => Equals(x.Box, task.Box) && Equals(x.OrganizationId, task.OrganizationId ?? x.OrganizationId));
      var exchangeStatus = boxLine.Status;
      if (exchangeStatus != Sungero.Parties.CounterpartyExchangeBoxes.Status.Closed)
      {
        var boxNotActiveMessage = Functions.BusinessUnitBox.CheckBusinessUnitBoxActive(task.Box);
        if (!string.IsNullOrWhiteSpace(boxNotActiveMessage))
        {
          e.AddError(boxNotActiveMessage);
          return;
        }
        
        if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null,
                                                                   Constants.IncomingInvitationTask.IncomingInvitationAssignmentConfirmDialogID.Accept))
          e.Cancel();
        
        var organizationId = boxLine.OrganizationId;
        var result = Functions.BusinessUnitBox.Remote.RejectInvitation(task.Box, task.Counterparty, organizationId, _obj.ActiveText);
        if (!string.IsNullOrWhiteSpace(result))
          e.AddError(result);
      }
      else
      {
        if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null,
                                                                   Constants.IncomingInvitationTask.IncomingInvitationAssignmentConfirmDialogID.Accept))
          e.Cancel();
      }
    }

    public virtual bool CanReject(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void Accept(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var task = IncomingInvitationTasks.As(_obj.Task);
      var boxLine = task.Counterparty.ExchangeBoxes.FirstOrDefault(x => Equals(x.Box, task.Box) && Equals(x.OrganizationId, task.OrganizationId ?? x.OrganizationId));
      var exchangeStatus = boxLine.Status;
      if (exchangeStatus != Sungero.Parties.CounterpartyExchangeBoxes.Status.Active)
      {
        var boxNotActiveMessage = Functions.BusinessUnitBox.CheckBusinessUnitBoxActive(task.Box);
        if (!string.IsNullOrWhiteSpace(boxNotActiveMessage))
        {
          e.AddError(boxNotActiveMessage);
          return;
        }
        
        if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null,
                                                                   Constants.IncomingInvitationTask.IncomingInvitationAssignmentConfirmDialogID.Accept))
          e.Cancel();
        
        var organizationId = boxLine.OrganizationId;
        var result = Functions.BusinessUnitBox.Remote.AcceptInvitation(task.Box, task.Counterparty, organizationId, _obj.ActiveText);
        if (!string.IsNullOrWhiteSpace(result))
          e.AddError(result);
      }
      else
      {
        if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null,
                                                                   Constants.IncomingInvitationTask.IncomingInvitationAssignmentConfirmDialogID.Accept))
          e.Cancel();
      }
    }

    public virtual bool CanAccept(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

  }

}