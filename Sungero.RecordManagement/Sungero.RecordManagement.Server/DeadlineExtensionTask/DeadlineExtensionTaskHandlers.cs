using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.DeadlineExtensionTask;

namespace Sungero.RecordManagement
{
  partial class DeadlineExtensionTaskServerHandlers
  {
    public override void BeforeRestart(Sungero.Workflow.Server.BeforeRestartEventArgs e)
    {
      // Заполнить текст задачи первой причиной.
      _obj.ActiveText = _obj.PrimaryReason;
      
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault());
    }

    public override void BeforeStart(Sungero.Workflow.Server.BeforeStartEventArgs e)
    {
      Docflow.PublicFunctions.Module.ValidateTaskAuthor(_obj, e);
      
      var assignmentsDeadLine = 1;
      _obj.MaxDeadline = Calendar.Now.AddWorkingDays(assignmentsDeadLine);
      
      // Проверить заполненность причины продления срока.
      if (string.IsNullOrWhiteSpace(_obj.ActiveText))
        e.AddError(DeadlineExtensionTasks.Resources.SpecifyReason);
      
      // Проверить корректность срока.
      if (!Docflow.PublicFunctions.Module.CheckDeadline(_obj.NewDeadline, Calendar.Now))
        e.AddError(_obj.Info.Properties.NewDeadline, RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanToday);
      
      // Новый срок поручения должен быть больше старого.
      if (e.IsValid && !Docflow.PublicFunctions.Module.CheckDeadline(_obj.NewDeadline, _obj.CurrentDeadline))
        e.AddError(_obj.Info.Properties.NewDeadline, DeadlineExtensionTasks.Resources.DesiredDeadlineIsNotCorrect);
      
      // Заполнить первоначальную причину и причину.
      _obj.PrimaryReason = _obj.ActiveText;
      _obj.Reason = _obj.ActiveText;
      
      // Выдать права на изменение для возможности прекращения задачи.
      Functions.ActionItemExecutionTask.GrantAccessRightToTask(_obj, _obj);
    }
  }
}