using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.SupAgreement;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts
{
  partial class SupAgreementClientHandlers
  {

    public override void Closing(Sungero.Presentation.FormClosingEventArgs e)
    {
      base.Closing(e);
      
      _obj.State.Properties.LeadingDocument.IsRequired = false;
    }

    public override void LeadingDocumentValueInput(Sungero.Docflow.Client.OfficialDocumentLeadingDocumentValueInputEventArgs e)
    {
      base.LeadingDocumentValueInput(e);
      
      if (e.NewValue != null)
      {
        if (Functions.SupAgreement.HaveDuplicates(_obj, _obj.BusinessUnit, _obj.RegistrationNumber, _obj.RegistrationDate, _obj.Counterparty, e.NewValue))
          e.AddWarning(ContractualDocuments.Resources.DuplicatesDetected + ContractualDocuments.Resources.FindDuplicates,
                       _obj.Info.Properties.Counterparty,
                       _obj.Info.Properties.BusinessUnit,
                       _obj.Info.Properties.RegistrationDate,
                       _obj.Info.Properties.RegistrationNumber,
                       _obj.Info.Properties.LeadingDocument);
      }
    }

    public override void CounterpartyValueInput(Sungero.Docflow.Client.ContractualDocumentBaseCounterpartyValueInputEventArgs e)
    {
      base.CounterpartyValueInput(e);
      
      if (e.NewValue != null)
      {
        if (Functions.SupAgreement.HaveDuplicates(_obj, _obj.BusinessUnit, _obj.RegistrationNumber, _obj.RegistrationDate, e.NewValue, _obj.LeadingDocument))
          e.AddWarning(ContractualDocuments.Resources.DuplicatesDetected + ContractualDocuments.Resources.FindDuplicates,
                       _obj.Info.Properties.Counterparty,
                       _obj.Info.Properties.BusinessUnit,
                       _obj.Info.Properties.RegistrationDate,
                       _obj.Info.Properties.RegistrationNumber,
                       _obj.Info.Properties.LeadingDocument);
      }
    }

    public override void BusinessUnitValueInput(Sungero.Docflow.Client.OfficialDocumentBusinessUnitValueInputEventArgs e)
    {
      base.BusinessUnitValueInput(e);
      
      if (e.NewValue != null)
      {
        if (Functions.SupAgreement.HaveDuplicates(_obj, e.NewValue, _obj.RegistrationNumber, _obj.RegistrationDate, _obj.Counterparty, _obj.LeadingDocument))
          e.AddWarning(ContractualDocuments.Resources.DuplicatesDetected + ContractualDocuments.Resources.FindDuplicates,
                       _obj.Info.Properties.Counterparty,
                       _obj.Info.Properties.BusinessUnit,
                       _obj.Info.Properties.RegistrationDate,
                       _obj.Info.Properties.RegistrationNumber,
                       _obj.Info.Properties.LeadingDocument);
      }
    }

  }
}