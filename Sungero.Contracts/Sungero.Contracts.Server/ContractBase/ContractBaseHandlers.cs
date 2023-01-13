using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ContractBase;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;

namespace Sungero.Contracts
{
  partial class ContractBaseConvertingFromServerHandler
  {

    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);
      
      e.Without(Sungero.Docflow.Addendums.Info.Properties.LeadingDocument);
    }
  }

  partial class ContractBaseDocumentGroupPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> DocumentGroupFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = base.DocumentGroupFiltering(query, e);
      query = query.Where(g => ContractCategories.Is(g));
      return query;
    }
  }

  partial class ContractBaseServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      if (!_obj.State.IsCopied)
      {
        _obj.IsAutomaticRenewal = false;
        var availableDocuments = Docflow.PublicFunctions.DocumentGroupBase.GetAvailableDocumentGroup(_obj.DocumentKind).Where(g => ContractCategories.Is(g));
        if (availableDocuments.Count() == 1)
          _obj.DocumentGroup = availableDocuments.FirstOrDefault();
      }
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);

      if (Functions.ContractBase.HaveDuplicates(_obj,
                                                _obj.BusinessUnit,
                                                _obj.RegistrationNumber,
                                                _obj.RegistrationDate,
                                                _obj.Counterparty))
        e.AddWarning(ContractualDocuments.Resources.DuplicatesDetected, _obj.Info.Actions.ShowDuplicates);
      
      if (_obj.DaysToFinishWorks < 0)
        e.AddError(_obj.Info.Properties.DaysToFinishWorks, ContractBases.Resources.IncorrectReminder);
      
      if (_obj.ValidFrom > _obj.ValidTill)
      {
        e.AddError(_obj.Info.Properties.ValidFrom, ContractBases.Resources.IncorrectValidDates, _obj.Info.Properties.ValidTill);
        e.AddError(_obj.Info.Properties.ValidTill, ContractBases.Resources.IncorrectValidDates, _obj.Info.Properties.ValidFrom);
      }
    }
  }

}