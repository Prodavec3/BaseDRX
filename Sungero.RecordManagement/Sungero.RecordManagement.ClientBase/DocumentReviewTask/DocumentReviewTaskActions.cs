using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.DocumentReviewTask;

namespace Sungero.RecordManagement.Client
{
  partial class DocumentReviewTaskActions
  {
    public override void Abort(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null, Constants.DocumentReviewTask.AbortConfirmDialogID))
        return;
      
      base.Abort(e);
    }

    public override bool CanAbort(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanAbort(e);
    }

    public virtual void AddResolution(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      PublicFunctions.DocumentReviewTask.AddResolution(_obj);
    }

    public virtual bool CanAddResolution(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status.Value == Workflow.Task.Status.Draft && Functions.DocumentReviewTask.HasDocumentAndCanRead(_obj);
    }

    public override void Start(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!RecordManagement.Functions.DocumentReviewTask.ValidateDocumentReviewTaskStart(_obj, e))
        return;
      
      // Вывести подтверждение удаления проектов резолюции.
      var dropDialogDescription = string.Empty;
      var dropDialogId = string.Empty;
      if (_obj.ResolutionGroup.ActionItemExecutionTasks.Any() &&
          !Functions.DocumentReviewTask.Remote.CanAuthorPrepareResolution(_obj))
      {
        // Инициатор не помощник адресата - удалить все.
        dropDialogDescription = Resources.ConfirmDeleteDraftResolutionAssignment;
        dropDialogId = Constants.DocumentReviewTask.StartWithDropConfirmDialogID;
      }
      else if (_obj.ResolutionGroup.ActionItemExecutionTasks.Where(x => _obj.Addressees.All(a => !Equals(a.Addressee, x.AssignedBy))).Any())
      {
        // Часть проектов выданы не тем адресатом, что указан в задаче - удалить неактуальные.
        dropDialogDescription = DocumentReviewTasks.Resources.ConfirmDeleteDraftResolutionsForWrongAddressee;
        dropDialogId = Constants.DocumentReviewTask.StartWithDropWrongActionItemsConfirmDialogID;
      }
      if (!string.IsNullOrEmpty(dropDialogDescription) && !string.IsNullOrEmpty(dropDialogId) &&
          !Functions.DocumentReviewTask.ShowDeletingDraftResolutionsConfirmationDialog(_obj, e.Action.ConfirmationMessage, dropDialogDescription, dropDialogId))
        return;
      
      // Вывести запрос прав на группу "Дополнительно".
      var grantRightDialogResult = Docflow.PublicFunctions.Module.ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList());
      if (grantRightDialogResult == false)
        return;
      
      // Вывести стандартный диалог подтверждения выполнения действия.
      if (_obj.NeedDeleteActionItems != true &&
          grantRightDialogResult == null &&
          !Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null, Constants.DocumentReviewTask.StartConfirmDialogID))
        return;
      
      base.Start(e);
    }

    public override bool CanStart(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanStart(e);
    }

  }
}