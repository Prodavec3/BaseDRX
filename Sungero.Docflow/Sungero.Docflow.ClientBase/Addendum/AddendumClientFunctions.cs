using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Addendum;

namespace Sungero.Docflow.Client
{
  partial class AddendumFunctions
  {
    /// <summary>
    /// Получить список типов документов, доступных для смены типа.
    /// </summary>
    /// <returns>Список типов документов, доступных для смены типа.</returns>
    public override List<Domain.Shared.IEntityInfo> GetTypesAvailableForChange()
    {
      var types = new List<Domain.Shared.IEntityInfo>();
      types.Add(Contracts.Contracts.Info);
      types.Add(Contracts.IncomingInvoices.Info);
      types.Add(Contracts.OutgoingInvoices.Info);
      types.Add(Contracts.SupAgreements.Info);
      types.Add(CounterpartyDocuments.Info);
      types.Add(FinancialArchive.ContractStatements.Info);
      types.Add(FinancialArchive.IncomingTaxInvoices.Info);
      types.Add(FinancialArchive.OutgoingTaxInvoices.Info);
      types.Add(FinancialArchive.UniversalTransferDocuments.Info);
      types.Add(FinancialArchive.Waybills.Info);
      types.Add(Meetings.Minuteses.Info);
      types.Add(Memos.Info);
      types.Add(Projects.ProjectDocuments.Info);
      types.Add(RecordManagement.CompanyDirectives.Info);
      types.Add(RecordManagement.IncomingLetters.Info);
      types.Add(RecordManagement.OutgoingLetters.Info);
      types.Add(RecordManagement.Orders.Info);
      types.Add(SimpleDocuments.Info);
      return types;
    }
    
    /// <summary>
    /// Сменить тип документа.
    /// </summary>
    /// <param name="types">Типы документов, на которые можно сменить.</param>
    /// <returns>Сконвертированный документ.</returns>
    [Public]
    public override IOfficialDocument ChangeDocumentType(List<Sungero.Domain.Shared.IEntityInfo> types)
    {
      var convertedDoc = base.ChangeDocumentType(types);
      
      // Не работает маппинг, если свойства нет в документе-источнике (115833).
      if (convertedDoc != null)
        Functions.OfficialDocument.FillLeadingDocument(convertedDoc, _obj.LeadingDocument);

      return convertedDoc;
    }
    
    /// <summary>
    /// Дополнительное условие доступности действия "Сменить тип".
    /// </summary>
    /// <returns>True - если действие "Сменить тип" доступно, иначе - false.</returns>
    public override bool CanChangeDocumentType()
    {
      return true;
    }
  }
}
