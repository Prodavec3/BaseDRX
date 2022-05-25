using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ApprovalIncInvoicePaidStage;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts.Server
{
  partial class ApprovalIncInvoicePaidStageFunctions
  {
    /// <summary>
    /// Смена статуса входящего счета в процессе согласования по регламенту.
    /// </summary>
    /// <param name="approvalTask">Задача на согласование по регламенту.</param>
    /// <returns>Результат выполнения кода.</returns>
    public override Docflow.Structures.ApprovalFunctionStageBase.ExecutionResult Execute(Sungero.Docflow.IApprovalTask approvalTask)
    {
      Logger.DebugFormat("ApprovalIncInvoicePaidStage. Start change incoming invoice state stage, approval task (ID={0}) (StartId={1}) (Iteration={2}) (StageNumber={3}).",
                         approvalTask.Id, approvalTask.StartId, approvalTask.Iteration, approvalTask.StageNumber);
      
      var document = approvalTask.DocumentGroup.OfficialDocuments.SingleOrDefault();
      if (document == null)
      {
        Logger.ErrorFormat("ApprovalIncInvoicePaidStage. Primary document not found. Approval task (ID={0}) (StartId={1}) (Iteration={2}) (StageNumber={3}).",
                           approvalTask.Id, approvalTask.StartId, approvalTask.Iteration, approvalTask.StageNumber);
        return this.GetErrorResult(Docflow.Resources.PrimaryDocumentNotFoundError);
      }
      
      if (Locks.GetLockInfo(document).IsLockedByOther)
      {
        Logger.DebugFormat("ApprovalIncInvoicePaidStage. Document locked. Approval task (ID={0}), Document (ID={1}), Locked By ({2}).",
                           approvalTask.Id, document.Id, Locks.GetLockInfo(document).OwnerName);
        return this.GetRetryResult(string.Empty);
      }
      
      try
      {
        if (!Sungero.Contracts.IncomingInvoices.Is(document))
        {
          Logger.DebugFormat("ApprovalIncInvoicePaidStage. Document is not incoming invoice, no need to change state. Approval task (ID={0}), Document (ID={1}).", 
                             approvalTask.Id, document.Id);
          return this.GetSuccessResult();
        }
        
        Logger.DebugFormat("ApprovalIncInvoicePaidStage. Set incoming invoice state to Paid. Approval task (ID={0}), Document (ID={1}), State = Paid.", approvalTask.Id, document.Id);
        var invoice = Sungero.Contracts.IncomingInvoices.As(document);
        Sungero.Contracts.Functions.IncomingInvoice.SetLifeCycleStateToPaid(invoice);
        invoice.Save();
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("ApprovalIncInvoicePaidStage. Set incoming invoice state error. Approval task (ID={0}) (Iteration={1}) (StageNumber={2}) for document (ID={3})",
                           ex, approvalTask.StartId, approvalTask.Iteration, approvalTask.StageNumber, document.Id);
        return this.GetRetryResult(string.Empty);
      }
      
      return this.GetSuccessResult();
    }
  }
}