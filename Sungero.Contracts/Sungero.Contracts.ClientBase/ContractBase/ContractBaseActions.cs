using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ContractBase;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts.Client
{
  partial class ContractBaseActions
  {

    public virtual void CreateContractStatement(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var contractStatement = Functions.Module.Remote.CreateContractStatement();
      contractStatement.LeadingDocument = _obj;
      contractStatement.Show();
    }

    public virtual bool CanCreateContractStatement(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && !_obj.State.IsChanged;
    }

    public virtual void CreateSupAgreement(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (Docflow.PublicFunctions.Module.Remote.IsModuleAvailableForCurrentUserByLicense(Sungero.Contracts.Constants.Module.ContractsUIGuid))
      {
        var supAgreement = Functions.Module.Remote.CreateSupAgreemnt();
        supAgreement.LeadingDocument = _obj;
        supAgreement.Show();
      }
      else
        e.AddWarning(Sungero.Contracts.Contracts.Resources.NoLicenceCreateSupAgreement);
    }

    public virtual bool CanCreateSupAgreement(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && !_obj.State.IsChanged;
    }

    public override void ShowDuplicates(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ShowDuplicates(e);

      var duplicates = Functions.ContractBase.Remote.GetDuplicates(_obj,
                                                                   _obj.BusinessUnit,
                                                                   _obj.RegistrationNumber,
                                                                   _obj.RegistrationDate,
                                                                   _obj.Counterparty);
      if (duplicates.Any())
        duplicates.Show();
      else
        Dialogs.NotifyMessage(ContractualDocuments.Resources.DuplicatesNotFound);
    }

    public override bool CanShowDuplicates(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanShowDuplicates(e);
    }

  }

}