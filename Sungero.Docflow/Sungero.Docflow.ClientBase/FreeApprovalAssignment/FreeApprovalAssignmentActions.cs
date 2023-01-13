using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalAssignment;

namespace Sungero.Docflow.Client
{
  partial class FreeApprovalAssignmentActions
  {
    public virtual void Forward(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (_obj.Addressee == null)
      {
        e.AddError(FreeApprovalTasks.Resources.CantRedirectWithoutAddressee);
        e.Cancel();
      }
      
      if (_obj.Addressee == _obj.Performer)
      {
        e.AddError(FreeApprovalAssignments.Resources.ApproverAlreadyExistsFormat(_obj.Addressee.Person.ShortName));
        e.Cancel();
      }
      
      if (!Functions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                              _obj.OtherGroup.All.ToList(),
                                                                              new List<IRecipient> { _obj.Addressee },
                                                                              e.Action,
                                                                              Constants.FreeApprovalTask.FreeApprovalAssignmentConfirmDialogID.Forward))
        e.Cancel();
    }

    public virtual bool CanForward(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Status == Status.InProcess && Functions.FreeApprovalTask.HasDocumentAndCanRead(FreeApprovalTasks.As(_obj.Task));
    }

    public virtual void AddApprover(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var dialog = Dialogs.CreateInputDialog(FreeApprovalTasks.Resources.AddApprover);
      var employee = dialog.AddSelect<Sungero.Company.IEmployee>(FreeApprovalTasks.Resources.Approver, true, null)
        .Where(x => x.Status != Company.Employee.Status.Closed);
      var defaultDeadline = Functions.Module.CheckDeadline(_obj.Deadline, Calendar.Now) ? _obj.Deadline : null;
      var deadline = dialog.AddDate(FreeApprovalTasks.Resources.AddApproverDeadline, _obj.Deadline.HasValue, defaultDeadline).AsDateTime();
      var addButton = dialog.Buttons.AddCustom(FreeApprovalTasks.Resources.Add);
      dialog.Buttons.AddCancel();
      dialog.SetOnButtonClick(a =>
                              {
                                if (a.IsValid && a.Button == addButton)
                                {
                                  if (Functions.FreeApprovalAssignment.Remote.CanForwardTo(_obj, employee.Value))
                                  {
                                    // Довыдаем права новому согласующему на вложения.
                                    if (Functions.Module.ShowDialogGrantAccessRights(_obj,
                                                                                     _obj.OtherGroup.All.ToList(),
                                                                                     new List<IRecipient>() { employee.Value }) == false)
                                    {
                                      a.CloseAfterExecute = false;
                                      return;
                                    }

                                    Docflow.Functions.Module.Remote.AddApprover(_obj, employee.Value, deadline.Value);
                                    Dialogs.NotifyMessage(FreeApprovalTasks.Resources.SendedToFormat(Company.PublicFunctions.Employee.GetShortName(employee.Value, DeclensionCase.Dative, false)));
                                  }
                                  else
                                    a.AddError(FreeApprovalAssignments.Resources.ApproverAlreadyExistsFormat(employee.Value.Person.ShortName));
                                }
                              });
      dialog.SetOnRefresh((r) =>
                          {
                            if (!Docflow.PublicFunctions.Module.CheckDeadline(employee.Value ?? Users.Current, deadline.Value, Calendar.Now))
                              r.AddError(FreeApprovalTasks.Resources.ImpossibleSpecifyDeadlineLessThanToday, deadline);
                            else
                            {
                              var warnMessage = Docflow.Functions.Module.CheckDeadlineByWorkCalendar(employee.Value ?? Users.Current, deadline.Value);
                              if (!string.IsNullOrEmpty(warnMessage))
                                r.AddWarning(warnMessage);
                            }
                          });
      var result = dialog.Show();
    }

    public virtual bool CanAddApprover(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Status.InProcess && _obj.AccessRights.CanUpdate() && 
        Functions.FreeApprovalTask.HasDocumentAndCanRead(FreeApprovalTasks.As(_obj.Task));
    }

    public virtual void ExtendDeadline(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var task = Docflow.PublicFunctions.DeadlineExtensionTask.Remote.GetDeadlineExtension(_obj);
      task.Show();
    }

    public virtual bool CanExtendDeadline(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Workflow.AssignmentBase.Status.InProcess && _obj.AccessRights.CanUpdate() && _obj.Deadline != null && 
        Functions.FreeApprovalTask.HasDocumentAndCanRead(FreeApprovalTasks.As(_obj.Task));
    }

    public virtual void Approved(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var accessRightsGranted = Functions.Module.ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList());
      if (accessRightsGranted == false)
        e.Cancel();
      
      if (accessRightsGranted == null && !Functions.FreeApprovalAssignment.ConfirmCompleteAssignment(_obj, e.Action))
        e.Cancel();
      
      // Подписание согласующей подписью с результатом "согласовано".
      Functions.Module.EndorseDocument(_obj, true, false, e);
      
    }

    public virtual bool CanApproved(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Addressee == null && Functions.FreeApprovalTask.HasDocumentAndCanRead(FreeApprovalTasks.As(_obj.Task));
    }

    public virtual void ForRework(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      // Валидация заполненности активного текста.
      if (!Functions.FreeApprovalTask.ValidateBeforeRework(_obj, FreeApprovalTasks.Resources.NeedTextForRework, e))
        e.Cancel();
      
      if (!Functions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, _obj.OtherGroup.All.ToList(), e.Action,
                                                                              Constants.FreeApprovalTask.FreeApprovalAssignmentConfirmDialogID.ForRework))
        e.Cancel();
      
      // Подписание согласующей подписью с результатом "не согласовано".
      Functions.Module.EndorseDocument(_obj, false, false, e);
    }

    public virtual bool CanForRework(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Addressee == null && Functions.FreeApprovalTask.HasDocumentAndCanRead(FreeApprovalTasks.As(_obj.Task));
    }

  }

}