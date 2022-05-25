using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Domain.Shared;
using Sungero.SmartProcessing.Structures.Module;

namespace Sungero.SmartProcessing.Client
{
  public class ModuleFunctions
  {
    
    #region Задача на верификацию
    
    /// <summary>
    /// Определить ведущий документ в комплекте.
    /// </summary>
    /// <param name="documents">Комплект документов.</param>
    /// <returns>Ведущий документ.</returns>
    [Public]
    public virtual IOfficialDocument GetLeadingDocument(List<IOfficialDocument> documents)
    {
      var documentPriority = new Dictionary<IOfficialDocument, int>();
      var documentTypePriorities = Functions.Module.GetPackageDocumentTypePriorities();
      int priority;
      foreach (var document in documents)
      {
        documentTypePriorities.TryGetValue(document.GetType().GetFinalType(), out priority);
        documentPriority.Add(document, priority);
      }
      
      var leadingDocument = documentPriority
        .OrderByDescending(p => p.Value)
        .FirstOrDefault().Key;
      return leadingDocument;
    }
    
    /// <summary>
    /// Вызвать диалог удаления документов.
    /// </summary>
    /// <param name="documentList">Документы для удаления.</param>
    /// <returns>Список ID удаленных документов.</returns>
    public static List<int> DeleteDocumentsDialogInWeb(List<IOfficialDocument> documentList)
    {
      var step = 1;
      var successfullyDeletedDocumentIds = new List<int>();
      var deleteWithExceptionDocuments = new List<IOfficialDocument>();
      
      var dialog = Dialogs.CreateInputDialog(VerificationAssignments.Resources.DeleteDocumentsDialogTitle);
      dialog.HelpCode = Constants.VerificationAssignment.HelpCodes.DeleteDocumentsDialog;
      dialog.Height = 80;
      
      var selectedDocuments = dialog
        .AddSelectMany(VerificationAssignments.Resources.DeleteDocumentsDialogAttachments, true, OfficialDocuments.Null)
        .From(documentList);
      selectedDocuments.IsVisible = false;
      var deleteButton = dialog.Buttons.AddCustom(Sungero.SmartProcessing.Resources.DeleteDocumentsDialogDeleteButtonName);
      deleteButton.IsVisible = false;
      
      Action showTroublesHandler = () =>
      {
        deleteWithExceptionDocuments.ShowModal();
      };
      var showTroubles = dialog.AddHyperlink(Sungero.SmartProcessing.Resources.DeleteDocumentDialogDeletingExceptionDocumentsHyperlinkTitle);
      showTroubles.SetOnExecute(showTroublesHandler);
      showTroubles.IsVisible = false;
      
      var cancelButton = dialog.Buttons.AddCancel();
      
      #region Dialog Refresh Handler
      
      Action<CommonLibrary.InputDialogRefreshEventArgs> refreshDialogHandler = (e) =>
      {
        if (step == 1)
        {
          selectedDocuments.IsVisible = true;
          deleteButton.IsVisible = true;
          dialog.Buttons.Default = deleteButton;
          showTroubles.IsVisible = false;
          dialog.Text = VerificationAssignments.Resources.DeleteDocumentsDialogText;
          cancelButton.Name = Sungero.SmartProcessing.Resources.DeleteDocumentsDialogCancelButtonName1;
        }
        else
        {
          selectedDocuments.IsVisible = false;
          deleteButton.IsVisible = false;
          showTroubles.IsVisible = true;
          dialog.Buttons.Default = cancelButton;
          var total = selectedDocuments.Value.Count();
          var failed = deleteWithExceptionDocuments.Count();
          var success = total - failed;
          dialog.Text = Sungero.SmartProcessing.Resources.DeleteDocumentsDialogDeletionTotalsFormat(total, Environment.NewLine);
          dialog.Text += Sungero.SmartProcessing.Resources.DeleteDocumentsDialogDeletionSuccessFormat(success, Environment.NewLine);
          dialog.Text += Sungero.SmartProcessing.Resources.DeleteDocumentsDialogDeletionFailedFormat(failed, Environment.NewLine);
          cancelButton.Name = Sungero.SmartProcessing.Resources.DeleteDocumentsDialogCancelButtonName2;
        }
      };
      
      dialog.SetOnRefresh(refreshDialogHandler);
      
      #endregion
      
      dialog.SetOnButtonClick((e) =>
                              {
                                if (e.Button == cancelButton)
                                  e.CloseAfterExecute = true;
                                
                                if (e.Button == deleteButton)
                                {
                                  if (!e.IsValid)
                                  {
                                    e.CloseAfterExecute = false;
                                    return;
                                  }
                                  
                                  successfullyDeletedDocumentIds = selectedDocuments.Value.Select(x => x.Id).ToList();
                                  
                                  deleteWithExceptionDocuments = TryDeleteDocuments(selectedDocuments.Value.ToList());
                                  if (deleteWithExceptionDocuments.Any())
                                  {
                                    e.CloseAfterExecute = false;
                                    step = 2;
                                  }
                                  else
                                  {
                                    e.CloseAfterExecute = true;
                                    Dialogs.NotifyMessage(VerificationAssignments.Resources.DeleteDocumentsDialogNoticeAfterDelete);
                                  }
                                  
                                  successfullyDeletedDocumentIds = successfullyDeletedDocumentIds.Where(x => !deleteWithExceptionDocuments.Any(y => y.Id == x)).ToList();
                                }
                              });
      
      dialog.Show();
      
      return successfullyDeletedDocumentIds;
    }
    
    /// <summary>
    /// Попытаться удалить документы.
    /// </summary>
    /// <param name="documents">Документы для удаления.</param>
    /// <returns>Документы, у которых возникли ошибки при удалении.</returns>
    public static List<IOfficialDocument> TryDeleteDocuments(List<IOfficialDocument> documents)
    {
      var deleteWithExceptionDocuments = new List<IOfficialDocument>();
      
      foreach (var document in documents)
      {
        var documentId = document.Id;
        try
        {
          Logger.DebugFormat("Verification Assignment. Action: DeleteDocuments. Try delete document: {0}", documentId);
          Sungero.Docflow.PublicFunctions.OfficialDocument.Remote.DeleteDocument(documentId);
          Logger.DebugFormat("Verification Assignment. Action: DeleteDocuments. Success. Document: {0}", documentId);
        }
        catch (Exception ex)
        {
          Logger.DebugFormat("Verification Assignment. Action: DeleteDocuments. Failed. Document: {0}{1}{2}", documentId, Environment.NewLine, ex);
          deleteWithExceptionDocuments.Add(document);
        }
      }
      
      return deleteWithExceptionDocuments;
    }
    
    #endregion
    
  }
}