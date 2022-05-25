using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.FinancialArchive.ContractStatement;

namespace Sungero.FinancialArchive
{
  partial class ContractStatementConvertingFromServerHandler
  {

    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);
      
      if (Sungero.Contracts.IncomingInvoices.Is(_source))
        e.Map(_info.Properties.LeadingDocument, Sungero.Contracts.IncomingInvoices.Info.Properties.Contract);
      
      e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.IsAdjustment);
    }
  }

  partial class ContractStatementServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      if (Functions.ContractStatement.HaveDuplicates(_obj,
                                                     _obj.BusinessUnit,
                                                     _obj.RegistrationNumber,
                                                     _obj.RegistrationDate,
                                                     _obj.Counterparty,
                                                     _obj.LeadingDocument))
        e.AddWarning(Contracts.ContractualDocuments.Resources.DuplicatesDetected, _obj.Info.Actions.ShowDuplicates);
    }
  }

}