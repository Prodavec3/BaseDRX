using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.FinancialArchive.OutgoingTaxInvoice;

namespace Sungero.FinancialArchive.Client
{
  partial class OutgoingTaxInvoiceFunctions
  {
    /// <summary>
    /// Получить список типов документов, доступных для смены типа.
    /// </summary>
    /// <returns>Список типов документов, доступных для смены типа.</returns>
    public override List<Domain.Shared.IEntityInfo> GetTypesAvailableForChange()
    {
      if (_obj.ExchangeState != null)
        return new List<Domain.Shared.IEntityInfo>() { Docflow.SimpleDocuments.Info };
      
      var types = new List<Domain.Shared.IEntityInfo>();
      types.Add(ContractStatements.Info);
      types.Add(IncomingTaxInvoices.Info);
      types.Add(UniversalTransferDocuments.Info);
      types.Add(Waybills.Info);
      types.Add(Contracts.IncomingInvoices.Info);
      return types;
    }
    
    /// <summary>
    /// Сменить тип документа.
    /// </summary>
    /// <param name="types">Типы документов, на которые можно сменить.</param>
    /// <returns>Сконвертированный документ.</returns>
    [Public]
    public override Sungero.Docflow.IOfficialDocument ChangeDocumentType(List<Domain.Shared.IEntityInfo> types)
    {
      Sungero.Docflow.IOfficialDocument convertedDoc = null;
      
      // Запретить смену типа, если документ или его тело заблокировано.
      var isCalledByDocument = CallContext.CalledDirectlyFrom(Docflow.OfficialDocuments.Info);
      if (isCalledByDocument && Docflow.PublicFunctions.Module.IsLockedByOther(_obj) ||
          !isCalledByDocument && Docflow.PublicFunctions.Module.IsLocked(_obj) ||
          Docflow.PublicFunctions.Module.VersionIsLocked(_obj.Versions.ToList()))
      {
        Dialogs.ShowMessage(Docflow.ExchangeDocuments.Resources.ChangeDocumentTypeLockError,
                            MessageType.Error);
        return convertedDoc;
      }
      
      // Открыть диалог по смене типа.
      var title = Docflow.ExchangeDocuments.Resources.TypeChange;
      var dialog = Dialogs.CreateSelectTypeDialog(title, types.ToArray());
      if (dialog.Show() == DialogButtons.Ok)
        convertedDoc = Docflow.OfficialDocuments.As(_obj.ConvertTo(dialog.SelectedType));
      
      // Котегов: Не работает маппинг, если свойства нет в документе-источнике (115833).
      if (convertedDoc != null)
        Docflow.PublicFunctions.OfficialDocument.FillLeadingDocument(convertedDoc, _obj.LeadingDocument);
      
      return convertedDoc;
    }

    /// <summary>
    /// Дополнительное условие доступности действия "Сменить тип".
    /// </summary>
    /// <returns>True - если действие "Сменить тип" доступно, иначе - false.</returns>
    public override bool CanChangeDocumentType()
    {
      return _obj.VerificationState == VerificationState.InProcess || base.CanChangeDocumentType();
    }
  }
}