using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;
using Sungero.Metadata;

namespace Sungero.SmartProcessing.Shared
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Получить приоритеты типов документов для определения ведущего документа в комплекте.
    /// </summary>
    /// <returns>Словарь с приоритетами типов.</returns>
    [Public]
    public virtual System.Collections.Generic.IDictionary<System.Type, int> GetPackageDocumentTypePriorities()
    {
      var leadingDocumentPriority = new Dictionary<System.Type, int>();
      leadingDocumentPriority.Add(typeof(RecordManagement.IIncomingLetter).GetFinalType(), 9);
      leadingDocumentPriority.Add(typeof(Contracts.IContract).GetFinalType(), 8);
      leadingDocumentPriority.Add(typeof(Contracts.ISupAgreement).GetFinalType(), 7);
      leadingDocumentPriority.Add(typeof(Sungero.FinancialArchive.IContractStatement).GetFinalType(), 6);
      leadingDocumentPriority.Add(typeof(Sungero.FinancialArchive.IWaybill).GetFinalType(), 5);
      leadingDocumentPriority.Add(typeof(Sungero.FinancialArchive.IUniversalTransferDocument).GetFinalType(), 4);
      leadingDocumentPriority.Add(typeof(Sungero.FinancialArchive.IIncomingTaxInvoice).GetFinalType(), 3);
      leadingDocumentPriority.Add(typeof(Sungero.Contracts.IIncomingInvoice).GetFinalType(), 2);
      leadingDocumentPriority.Add(typeof(Sungero.FinancialArchive.IOutgoingTaxInvoice).GetFinalType(), 1);
      leadingDocumentPriority.Add(typeof(Docflow.ISimpleDocument).GetFinalType(), 0);
      
      return leadingDocumentPriority;
    }
  }
}