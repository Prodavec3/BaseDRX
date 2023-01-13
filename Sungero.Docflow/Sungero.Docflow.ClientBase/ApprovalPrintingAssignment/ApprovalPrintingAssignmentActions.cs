using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalPrintingAssignment;

namespace Sungero.Docflow.Client
{
  partial class ApprovalPrintingAssignmentActions
  {
    public virtual void ForRevision(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      // Валидация заполнения ответственного за доработку.
      if (_obj.ReworkPerformer == null)
      {
        e.AddError(ApprovalTasks.Resources.CantSendForReworkWithoutPerformer);
        e.Cancel();
      }
      
      // Проверить заполненность активного текста.
      if (!Functions.ApprovalTask.ValidateBeforeRework(_obj, ApprovalTasks.Resources.NeedTextForRework, e))
        e.Cancel();
      
      // Вызов диалога выбора исполнителя или подтверждения действия и диалога запроса выдачи прав на вложения (при отсутствии прав).
      Functions.ApprovalTask.ShowReworkConfirmationDialog(ApprovalTasks.As(_obj.Task), _obj, _obj.OtherGroup.All.ToList(), new List<IRecipient>(), _obj.ReworkPerformer, e,
                                                          Constants.ApprovalTask.ApprovalPrintingAssignmentConfirmDialogID.ForRevision);
    }

    public virtual bool CanForRevision(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task));
    }

    public virtual void SendByMail(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.ApprovalSendingAssignment.SendByMail(ApprovalTasks.As(_obj.Task));
    }

    public virtual bool CanSendByMail(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.DocumentGroup.OfficialDocuments.Any(d => d.HasVersions);
    }

    public virtual void SendViaExchangeService(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      Functions.ApprovalSendingAssignment.SendToCounterparty(document, _obj.Task);
    }

    public virtual bool CanSendViaExchangeService(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      return _obj.DocumentGroup.OfficialDocuments.Any() && Functions.ApprovalSendingAssignment.CanSendToCounterparty(document);
    }

    public virtual void Execute(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      var sendDialog = Functions.Module.ShowConfirmationDialogSendToCounterparty(_obj, _obj.CollapsedStagesTypesPr.Select(x => x.StageType), e);
      if (!Functions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                              _obj.OtherGroup.All.ToList(),
                                                                              sendDialog ? null : e.Action,
                                                                              Constants.ApprovalTask.ApprovalPrintingAssignmentConfirmDialogID.Execute))
        e.Cancel();
    }

    public virtual bool CanExecute(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.DocumentGroup.OfficialDocuments.Any();
    }

    public virtual void CreateCoverLetter(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      Functions.ApprovalSendingAssignment.CreateCoverLetter(_obj.DocumentGroup.OfficialDocuments.FirstOrDefault(), _obj.OtherGroup);
    }

    public virtual bool CanCreateCoverLetter(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      return _obj.Status == Workflow.Task.Status.InProcess && _obj.DocumentGroup.OfficialDocuments.Any() && Functions.ApprovalTask.EnableCreateCoverLetter(document);
    }

    public virtual void ApprovalForm(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      var document = _obj.DocumentGroup.OfficialDocuments.Single();
      Functions.Module.RunApprovalSheetReport(document);
    }

    public virtual bool CanApprovalForm(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.DocumentGroup.OfficialDocuments.Any(d => d.HasVersions);
    }

  }
}