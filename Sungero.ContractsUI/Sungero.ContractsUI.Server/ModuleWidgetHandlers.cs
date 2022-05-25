using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.ContractsUI.Server
{

  partial class MyContractsWidgetHandlers
  {

    public virtual IQueryable<Sungero.Contracts.IContractualDocument> MyContractsOnApprovalFiltering(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      return Functions.Module.GetMyContractualDocuments(query, _parameters.Substitution, _parameters.Show);
    }

    public virtual IQueryable<Sungero.Contracts.IContractualDocument> MyContractsCompletedFiltering(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      return Functions.Module.GetMyExpiringSoonContracts(query, null, _parameters.Substitution, _parameters.Show);
    }

    public virtual IQueryable<Sungero.Contracts.IContractualDocument> MyContractsIsNotAutomaticRenewalFiltering(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      return Functions.Module.GetMyExpiringSoonContracts(query, false, _parameters.Substitution, _parameters.Show);
    }

    public virtual IQueryable<Sungero.Contracts.IContractualDocument> MyContractsIsAutomaticRenewalFiltering(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      return Functions.Module.GetMyExpiringSoonContracts(query, true, _parameters.Substitution, _parameters.Show);
    }
  }

}