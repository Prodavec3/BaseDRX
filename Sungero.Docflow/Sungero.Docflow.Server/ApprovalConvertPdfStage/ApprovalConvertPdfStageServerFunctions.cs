using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalConvertPdfStage;

namespace Sungero.Docflow.Server
{
  partial class ApprovalConvertPdfStageFunctions
  {

    public override Docflow.Structures.ApprovalFunctionStageBase.ExecutionResult Execute(IApprovalTask approvalTask)
    {
      Logger.DebugFormat("ApprovalConvertPdfStage. Start execute convert to pdf for task id: {0}, start id: {1}.", approvalTask.Id, approvalTask.StartId);
      
      var result = base.Execute(approvalTask);
      
      var documents = new List<IOfficialDocument>();
      
      var documentFromTask = approvalTask.DocumentGroup.OfficialDocuments.SingleOrDefault();
      if (documentFromTask == null)
      {
        Logger.ErrorFormat("ApprovalConvertPdfStage. Primary document not found. task id: {0}, start id: {1}", approvalTask.Id, approvalTask.StartId);
        return this.GetErrorResult(Sungero.Docflow.Resources.PrimaryDocumentNotFoundError);
      }
      
      documents.Add(documentFromTask);
      if (_obj.ConvertWithAddenda == true)
      {
        var addenda = approvalTask.AddendaGroup.OfficialDocuments.ToList();
        documents.AddRange(addenda);
      }
      
      var documentsToConvert = new List<IOfficialDocument>();
      foreach (var document in documents)
      {
        if (!document.HasVersions)
        {
          Logger.DebugFormat("ApprovalConvertPdfStage. Document with Id {0} has no version.", document.Id);
          continue;
        }
        
        // Документ МКДО.
        if (Functions.OfficialDocument.IsExchangeDocument(document, document.LastVersion.Id))
        {
          Logger.DebugFormat("ApprovalConvertPdfStage. Document with Id {0} is exchange document. Skipped converting to PDF.", document.Id);
          continue;
        }
        
        // Формат не поддерживается.
        var versionExtension = document.LastVersion.BodyAssociatedApplication.Extension.ToLower();
        var versionExtensionIsSupported = AsposeExtensions.Converter.CheckIfExtensionIsSupported(versionExtension);
        if (!versionExtensionIsSupported)
        {
          Logger.DebugFormat("ApprovalConvertPdfStage. Document with Id {0} unsupported format {1}.", document.Id, versionExtension);
          continue;
        }
        
        var lockInfo = Locks.GetLockInfo(document.LastVersion.Body);
        if (lockInfo.IsLocked)
        {
          Logger.DebugFormat("ApprovalConvertPdfStage. Document with Id {0} locked {1}.", document.Id, lockInfo.OwnerName);
          return this.GetRetryResult(string.Format(Sungero.Docflow.ApprovalConvertPdfStages.Resources.ConvertPdfLockError, document.Name, document.Id, lockInfo.OwnerName));
        }
        
        documentsToConvert.Add(document);
      }
      
      foreach (var document in documentsToConvert)
      {
        try
        {
          Logger.DebugFormat("ApprovalConvertPdfStage. Start convert to pdf for document id {0}.", document.Id);
          var convertionResult = Functions.ApprovalConvertPdfStage.ConvertToPdf(_obj, document);
          if (convertionResult.HasErrors)
          {
            Logger.ErrorFormat("ApprovalConvertPdfStage. Convert to pdf error {0}. Document Id {1}, Version Id {2}", convertionResult.ErrorMessage, document.Id, document.LastVersion.Id);
            result = this.GetRetryResult(string.Empty);
          }
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("ApprovalConvertPdfStage. Convert to pdf error. Document Id {0}, Version Id {1}", ex, document.Id, document.LastVersion.Id);
          result = this.GetRetryResult(string.Empty);
        }
      }
      
      Logger.DebugFormat("ApprovalConvertPdfStage. Done execute convert to pdf for task id {0}, success: {1}, retry: {2}", approvalTask.Id, result.Success, result.Retry);
      
      return result;
    }
    
    /// <summary>
    /// Преобразовать тело документа в формат pdf.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Информация о результате генерации PublicBody для версии документа.</returns>
    public virtual Structures.OfficialDocument.СonversionToPdfResult ConvertToPdf(IOfficialDocument document)
    {
      var lastVersionId = document.LastVersion.Id;
      var signature = Functions.OfficialDocument.GetSignatureForMark(document, lastVersionId);
      var signatureMark = (signature != null) ? Functions.OfficialDocument.GetSignatureMarkAsHtml(document, lastVersionId) : string.Empty;
      
      return Functions.Module.GeneratePublicBodyWithSignatureMark(document, lastVersionId, signatureMark);
    }
  }
}