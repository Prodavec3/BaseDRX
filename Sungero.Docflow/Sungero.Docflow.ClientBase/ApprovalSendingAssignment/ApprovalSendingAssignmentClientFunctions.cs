using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalSendingAssignment;

namespace Sungero.Docflow.Client
{
  partial class ApprovalSendingAssignmentFunctions
  {
    #region Отправка по email
    
    /// <summary>
    /// Создать и отправить письмо по почте.
    /// </summary>
    /// <param name="task">Задача.</param>
    public static void SendByMail(IApprovalTask task)
    {
      var doc = task.DocumentGroup.OfficialDocuments.FirstOrDefault();
      
      var addenda = task.AddendaGroup.OfficialDocuments.Where(x => !Equals(x, OfficialDocuments.Null) && x.HasVersions).ToList();
      var other = task.OtherGroup.All.Where(x => OfficialDocuments.Is(x)).Cast<IOfficialDocument>().ToList();
      other = other.Where(x => !Equals(x, OfficialDocuments.Null) && x.HasVersions).ToList();
      
      var relatedDocuments = new List<IOfficialDocument>();
      relatedDocuments.AddRange(addenda);
      relatedDocuments.AddRange(other);
      
      Sungero.Docflow.Functions.OfficialDocument.SelectRelatedDocumentsAndCreateEmail(doc, relatedDocuments);
    }
    
    #endregion
    
    /// <summary>
    /// Создать сопроводительное письмо.
    /// </summary>
    /// <param name="document">Документ, к которому создается сопроводительное письмо.</param>
    /// <param name="attachmentsGroup">Группа вложений.</param>
    public static void CreateCoverLetter(IOfficialDocument document, Workflow.Interfaces.IWorkflowEntityAttachmentGroup attachmentsGroup)
    {
      // TODO Reshetnikov_MA переполучаем документ для обновления связей в десктоп, #71595.
      var officialDocument = Docflow.PublicFunctions.OfficialDocument.Remote.GetOfficialDocument(document.Id);
      
      var correspondence = officialDocument.Relations.GetRelated(Constants.Module.CorrespondenceRelationName)
        .Where(r => attachmentsGroup.All.Contains(r)).ToList();
      
      if (correspondence.Count == 1)
      {
        var dialog = Dialogs.CreateTaskDialog(ApprovalSendingAssignments.Resources.LetterAlreadyExist,
                                              ApprovalSendingAssignments.Resources.LetterAlreadyExistQuestion,
                                              MessageType.Question);
        dialog.Buttons.AddYesNo();
        dialog.Buttons.Default = DialogButtons.No;
        if (dialog.Show() == DialogButtons.Yes)
          correspondence.First().ShowModal();
      }
      else if (correspondence.Any())
      {
        Dialogs.ShowMessage(ApprovalSendingAssignments.Resources.LetterAlreadyExists, MessageType.Information);
      }
      else
      {
        var letter = Functions.Module.CreateCoverLetter(document);
        if (letter == null)
          return;
        
        // Открываем модально, чтобы следующая проверка прошла уже после того, как мы закончили создавать письмо, иначе условие не выполнится никогда.
        letter.ShowModal();
        // Если письмо сохранили - добавляем в задание.
        if (!letter.State.IsChanged)
          attachmentsGroup.All.Add(letter);
      }
    }

    /// <summary>
    /// Отправка документа, либо ответа контрагенту с учетом выбранного сервиса обмена и приложений в задаче на согласование.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача.</param>
    public static void SendToCounterparty(IOfficialDocument document, Workflow.ITask task)
    {
      var approvalTask = ApprovalTasks.As(task);
      var addenda = approvalTask.AddendaGroup.OfficialDocuments.ToList();
      
      Exchange.PublicFunctions.Module.SendResultToCounterparty(document, approvalTask.ExchangeService, addenda);
    }
    
    /// <summary>
    /// Проверить возможность отправки документа контрагенту.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если можно отправить, иначе - false.</returns>
    public static bool CanSendToCounterparty(IOfficialDocument document)
    {
      return !document.State.IsInserted && !document.State.IsChanged && document.AccessRights.CanUpdate() &&
        document.AccessRights.CanSendByExchange() && document.HasVersions;
    }
  }
}