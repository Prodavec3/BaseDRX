using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.SupAgreement;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts.Client
{
  partial class SupAgreementActions
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

    public override void SendForApproval(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.SendForApproval(e);
    }

    public override bool CanSendForApproval(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSendForApproval(e);
    }

    public override void ShowDuplicates(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ShowDuplicates(e);
      
      var duplicates = Functions.SupAgreement.Remote.GetDuplicates(_obj,
                                                                   _obj.BusinessUnit,
                                                                   _obj.RegistrationNumber,
                                                                   _obj.RegistrationDate,
                                                                   _obj.Counterparty,
                                                                   _obj.LeadingDocument);
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