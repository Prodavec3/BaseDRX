using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalTask;

namespace Sungero.Docflow.Client
{
  partial class ApprovalTaskActions
  {
    public override void Restart(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var description = string.Empty;
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
      {
        var createdTasks = Docflow.PublicFunctions.Module.Remote.GetApprovalTasks(document);
        if (createdTasks.Any())
          description = OfficialDocuments.Resources.DocumentHasApprovalTasks;
      }
      
      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, description, null,
                                                                 Constants.ApprovalTask.RestartConfirmDialogID))
        return;
      base.Restart(e);
    }

    public override bool CanRestart(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanRestart(e);
    }

    public virtual void ApprovalForm(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(_obj))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      Docflow.Functions.Module.RunApprovalSheetReport(_obj.DocumentGroup.OfficialDocuments.Single());
    }

    public virtual bool CanApprovalForm(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.DocumentGroup.OfficialDocuments.Any() && _obj.DocumentGroup.OfficialDocuments.Single().HasVersions;
    }

    public override void Abort(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (Functions.ApprovalTask.GetReasonBeforeAbort(_obj, null, e, true))
      {
        base.Abort(e);
        Functions.ApprovalTask.AbortAsyncProcessingNotify(_obj);
      }
    }

    public override bool CanAbort(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanAbort(e);
    }

    public override void Start(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(_obj))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      if (!Sungero.Docflow.Functions.ApprovalTask.ClientValidateApprovalTaskStart(_obj, e))
        return;
      
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
      {
        // Если инициатор указан в этапе согласования, то проверяем наличие сертификата.
        var approvalStage = Functions.ApprovalTask.Remote.AuthorMustApproveDocument(_obj, _obj.Author, _obj.AddApprovers.Select(app => app.Approver).ToList());
        var documentHasBody = document.Versions.Any();
        
        if (approvalStage.HasApprovalStage && documentHasBody && approvalStage.NeedStrongSign && !PublicFunctions.Module.Remote.GetCertificates(document).Any())
        {
          e.AddError(ApprovalTasks.Resources.CertificateNeeded);
          return;
        }
        
        var giveRights = Functions.Module.ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList(), _obj.AddApprovers.Select(a => a.Approver).ToList());
        if (giveRights == false)
          return;

        var author = _obj.Author;
        
        if (giveRights == null)
        {
          // Запросить подтверждение подписания и отправки.
          var question = ApprovalTasks.Resources.AreYouSureYouWantSendDocumentForApproval;
          if (approvalStage.HasApprovalStage)
          {
            if (documentHasBody)
              question = ApprovalTasks.Resources.AreYouSureYouWantSignAndSendDocumentForApproval;
            if (_obj.AddendaGroup.OfficialDocuments.Any())
              question = ApprovalTasks.Resources.AreYouSureYouWantSignAndSendDocumentAndAddendaForApproval;
          }
          var dialogResult = Functions.Module.ShowConfirmationDialog(question, null, null, Constants.ApprovalTask.StartConfirmDialogID);
          if (!dialogResult)
            return;
        }
        
        if (approvalStage.HasApprovalStage)
        {
          try
          {
            // Если инициатор указан в этапе согласования, то установить его подпись сразу.
            if (!Functions.Module.EndorseWithAddenda(document, _obj.AddendaGroup.OfficialDocuments.ToList<IElectronicDocument>(), null, author, approvalStage.NeedStrongSign, string.Empty))
            {
              e.AddError(ApprovalTasks.Resources.ToStartNeedSignDocument);
              return;
            }
          }
          catch (CommonLibrary.Exceptions.PlatformException ex)
          {
            Logger.ErrorFormat("Start task id = {0}. Failed to endorse document with addenda. Document id = '{1}' ", ex, _obj.Id, document.Id);
            if (!ex.IsInternal)
            {
              var message = string.Format("{0}.", ex.Message.TrimEnd('.'));
              e.AddError(message);
              return;
            }
            else
              throw;
          }
        }
      }
      
      base.Start(e);
    }

    public override bool CanStart(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanStart(e);
    }

  }
}