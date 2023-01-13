using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.AcquaintanceAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class AcquaintanceAssignmentActions
  {
    public virtual void ExtendDeadline(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var task = Docflow.PublicFunctions.DeadlineExtensionTask.Remote.GetDeadlineExtension(_obj);
      task.Show();
    }

    public virtual bool CanExtendDeadline(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Workflow.AssignmentBase.Status.InProcess &&
        _obj.AccessRights.CanUpdate() &&
        Functions.AcquaintanceTask.HasDocumentAndCanRead(AcquaintanceTasks.As(_obj.Task));
    }

    public virtual void Acquainted(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.AcquaintanceTask.HasDocumentAndCanRead(AcquaintanceTasks.As(_obj.Task)))
      {
        e.AddError(Docflow.Resources.NoRightsToDocument);
        return;
      }

      // Проверка прав на выполнение по замещению.
      var isCurrentUserPerformer = Equals(_obj.Performer, Users.Current);
      if (!isCurrentUserPerformer)
      {
        // Задание может выполнить только замещающий, и только за виртуальных или уволенных сотрудников,
        // либо за действующего сотрудника, если это разрешено соответствующей настройкой.
        if (!Functions.AcquaintanceAssignment.CanUserCompleteAcquaintanceBySubstitute(_obj))
        {
          e.AddError(Sungero.RecordManagement.AcquaintanceAssignments.Resources.EmployeeMustPersonallyConfirmAcquaintance);
          return;
        }
        
        // Требовать оставить комментарий при выполнении по замещению.
        if (string.IsNullOrWhiteSpace(_obj.ActiveText))
        {
          e.AddError(AcquaintanceTasks.Resources.CompletedBySubstitution);
          return;
        }
      }
      else
      {
        // Требовать прочтения и валидировать подпись, только если сотрудник лично выполняет задание.
        var task = AcquaintanceTasks.As(_obj.Task);
        var isElectronicAcquaintance = task.IsElectronicAcquaintance.Value;
        if (isElectronicAcquaintance)
        {
          // Требовать прочтение отправленной версии документа.
          var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
          var acquaintanceVersion = task.AcquaintanceVersions.FirstOrDefault(x => x.IsMainDocument.Value);
          var acquaintanceVersionNumber = acquaintanceVersion.Number.Value;
          if (!Functions.AcquaintanceTask.Remote.IsDocumentVersionReaded(document, acquaintanceVersionNumber))
          {
            var error = document.LastVersion.Number == acquaintanceVersionNumber
              ? AcquaintanceTasks.Resources.DocumentNotReadedLastVersion
              : AcquaintanceTasks.Resources.DocumentNotReadedFormat(acquaintanceVersionNumber);
            e.AddError(error);
            return;
          }
          
          // Валидация подписи.
          if (!Functions.AcquaintanceTask.Remote.IsDocumentVersionSignatureValid(document, acquaintanceVersionNumber))
          {
            e.AddError(AcquaintanceTasks.Resources.DocumentSignatureNotValid);
            return;
          }
        }
      }
      
      // Замена стандартного диалога подтверждения выполнения действия.
      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null,
                                                                 Constants.AcquaintanceTask.AcquaintedConfirmDialogID))
        e.Cancel();
    }

    public virtual bool CanAcquainted(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.DocumentGroup.OfficialDocuments.Any();
    }
  }
}