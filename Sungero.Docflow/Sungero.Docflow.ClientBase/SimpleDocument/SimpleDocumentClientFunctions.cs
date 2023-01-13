using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.SimpleDocument;

namespace Sungero.Docflow.Client
{
  partial class SimpleDocumentFunctions
  {
    /// <summary>
    /// Получить список типов документов, доступных для смены типа.
    /// </summary>
    /// <returns>Список типов документов, доступных для смены типа.</returns>
    public override List<Domain.Shared.IEntityInfo> GetTypesAvailableForChange()
    {
      var types = new List<Domain.Shared.IEntityInfo>();
      types.Add(Addendums.Info);
      types.Add(Memos.Info);
      types.Add(Meetings.Minuteses.Info);
      types.Add(Contracts.Contracts.Info);
      types.Add(Sungero.FinancialArchive.ContractStatements.Info);
      types.Add(Contracts.IncomingInvoices.Info);
      types.Add(Contracts.OutgoingInvoices.Info);
      types.Add(Contracts.SupAgreements.Info);
      types.Add(Projects.ProjectDocuments.Info);
      types.Add(RecordManagement.IncomingLetters.Info);
      types.Add(RecordManagement.OutgoingLetters.Info);
      types.Add(RecordManagement.Orders.Info);
      types.Add(RecordManagement.CompanyDirectives.Info);
      types.Add(CounterpartyDocuments.Info);
      types.Add(Sungero.FinancialArchive.IncomingTaxInvoices.Info);
      types.Add(Sungero.FinancialArchive.OutgoingTaxInvoices.Info);
      types.Add(Sungero.FinancialArchive.UniversalTransferDocuments.Info);
      types.Add(Sungero.FinancialArchive.Waybills.Info);
      return types;
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