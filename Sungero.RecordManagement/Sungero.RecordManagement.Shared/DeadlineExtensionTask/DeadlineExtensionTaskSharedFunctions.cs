using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.DeadlineExtensionTask;

namespace Sungero.RecordManagement.Shared
{
  partial class DeadlineExtensionTaskFunctions
  {

    /// <summary>
    /// Получить тему задачи на продление срока.
    /// </summary>
    /// <param name="task">Задача "Продление срока".</param>
    /// <param name="beginningSubject">Начальная тема задачи.</param>
    /// <returns>Сформированная тема задачи.</returns>
    public static string GetDeadlineExtensionSubject(Sungero.RecordManagement.IDeadlineExtensionTask task, CommonLibrary.LocalizedString beginningSubject)
    {
      // Добавить ">> " т.к. подзадача.
      using (TenantInfo.Culture.SwitchTo())
      {
        var subject = string.Format(">> {0}", beginningSubject);
        
        if (!string.IsNullOrWhiteSpace(task.ActionItem))
        {
          var resolution = Functions.ActionItemExecutionTask.FormatActionItemForSubject(task.ActionItem, task.DocumentsGroup.OfficialDocuments.Any());
          subject += string.Format(" {0}", resolution);
        }
        
        // Добавить имя документа, если поручение с документом.
        var document = task.DocumentsGroup.OfficialDocuments.FirstOrDefault();
        if (document != null)
          subject += ActionItemExecutionTasks.Resources.SubjectWithDocumentFormat(document.Name);
        
        return Docflow.PublicFunctions.Module.TrimSpecialSymbols(subject);
      }
    }
  }
}