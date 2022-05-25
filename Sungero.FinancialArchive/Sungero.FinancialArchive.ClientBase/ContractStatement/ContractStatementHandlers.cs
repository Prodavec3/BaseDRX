using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.FinancialArchive.ContractStatement;

namespace Sungero.FinancialArchive
{
  partial class ContractStatementClientHandlers
  {

    public override void LeadingDocumentValueInput(Sungero.Docflow.Client.OfficialDocumentLeadingDocumentValueInputEventArgs e)
    {
      base.LeadingDocumentValueInput(e);
      
      if (e.NewValue != null)
      {
        if (Functions.ContractStatement.HaveDuplicates(_obj, _obj.BusinessUnit, _obj.RegistrationNumber, _obj.RegistrationDate, _obj.Counterparty, e.NewValue))
          e.AddWarning(Contracts.ContractualDocuments.Resources.DuplicatesDetected + Contracts.ContractualDocuments.Resources.FindDuplicates,
                       _obj.Info.Properties.LeadingDocument,
                       _obj.Info.Properties.Counterparty,
                       _obj.Info.Properties.BusinessUnit,
                       _obj.Info.Properties.RegistrationNumber,
                       _obj.Info.Properties.RegistrationDate);
      }
      
      this._obj.State.Properties.LeadingDocument.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override void CounterpartyValueInput(Sungero.Docflow.Client.AccountingDocumentBaseCounterpartyValueInputEventArgs e)
    {
      base.CounterpartyValueInput(e);
      
      if (e.NewValue != null)
      {
        if (Functions.ContractStatement.HaveDuplicates(_obj, _obj.BusinessUnit, _obj.RegistrationNumber, _obj.RegistrationDate, e.NewValue, _obj.LeadingDocument))
          e.AddWarning(Contracts.ContractualDocuments.Resources.DuplicatesDetected + Contracts.ContractualDocuments.Resources.FindDuplicates,
                       _obj.Info.Properties.LeadingDocument,
                       _obj.Info.Properties.Counterparty,
                       _obj.Info.Properties.BusinessUnit,
                       _obj.Info.Properties.RegistrationNumber,
                       _obj.Info.Properties.RegistrationDate);
      }
    }

    public override void BusinessUnitValueInput(Sungero.Docflow.Client.OfficialDocumentBusinessUnitValueInputEventArgs e)
    {
      base.BusinessUnitValueInput(e);
      
      if (e.NewValue != null)
      {
        if (Functions.ContractStatement.HaveDuplicates(_obj, e.NewValue, _obj.RegistrationNumber, _obj.RegistrationDate, _obj.Counterparty, _obj.LeadingDocument))
          e.AddWarning(Contracts.ContractualDocuments.Resources.DuplicatesDetected + Contracts.ContractualDocuments.Resources.FindDuplicates,
                       _obj.Info.Properties.LeadingDocument,
                       _obj.Info.Properties.Counterparty,
                       _obj.Info.Properties.BusinessUnit,
                       _obj.Info.Properties.RegistrationNumber,
                       _obj.Info.Properties.RegistrationDate);
      }
    }

    public override void RegistrationNumberValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      base.RegistrationNumberValueInput(e);
      
      if (!string.IsNullOrEmpty(e.NewValue))
      {
        if (Functions.ContractStatement.HaveDuplicates(_obj, _obj.BusinessUnit, e.NewValue, _obj.RegistrationDate, _obj.Counterparty, _obj.LeadingDocument))
          e.AddWarning(Contracts.ContractualDocuments.Resources.DuplicatesDetected + Contracts.ContractualDocuments.Resources.FindDuplicates,
                       _obj.Info.Properties.LeadingDocument,
                       _obj.Info.Properties.Counterparty,
                       _obj.Info.Properties.BusinessUnit,
                       _obj.Info.Properties.RegistrationNumber,
                       _obj.Info.Properties.RegistrationDate);
      }
      
      this._obj.State.Properties.RegistrationNumber.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override void RegistrationDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      base.RegistrationDateValueInput(e);
      
      if (e.NewValue != null && e.NewValue >= Calendar.SqlMinValue)
      {
        if (Functions.ContractStatement.HaveDuplicates(_obj, _obj.BusinessUnit, _obj.RegistrationNumber, e.NewValue, _obj.Counterparty, _obj.LeadingDocument))
          e.AddWarning(Contracts.ContractualDocuments.Resources.DuplicatesDetected + Contracts.ContractualDocuments.Resources.FindDuplicates,
                       _obj.Info.Properties.LeadingDocument,
                       _obj.Info.Properties.Counterparty,
                       _obj.Info.Properties.BusinessUnit,
                       _obj.Info.Properties.RegistrationNumber,
                       _obj.Info.Properties.RegistrationDate);
      }
      
      this._obj.State.Properties.RegistrationDate.HighlightColor = Sungero.Core.Colors.Empty;
    }

  }
}