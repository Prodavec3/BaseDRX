using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Domain.Client;
using Sungero.FinancialArchive.ContractStatement;

namespace Sungero.FinancialArchive.Client
{
  partial class ContractStatementActions
  {
    
    public virtual void CreateCoverLetter(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var letter = Docflow.PublicFunctions.Module.CreateCoverLetter(_obj);
      letter.Show();
    }

    public virtual bool CanCreateCoverLetter(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && !_obj.State.IsChanged;
    }

    public virtual void ShowDuplicates(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var duplicates = Functions.ContractStatement.Remote.GetDuplicates(_obj,
                                                                        _obj.BusinessUnit,
                                                                        _obj.RegistrationNumber,
                                                                        _obj.RegistrationDate,
                                                                        _obj.Counterparty,
                                                                        _obj.LeadingDocument);
      if (duplicates.Any())
        duplicates.Show();
      else
        Dialogs.NotifyMessage(Contracts.ContractualDocuments.Resources.DuplicatesNotFound);
    }

    public virtual bool CanShowDuplicates(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }

}