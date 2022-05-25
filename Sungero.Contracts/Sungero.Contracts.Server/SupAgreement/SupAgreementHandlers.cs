using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.SupAgreement;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;

namespace Sungero.Contracts
{
  partial class SupAgreementConvertingFromServerHandler
  {
    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);
      
      e.Without(Sungero.Contracts.Contracts.Info.Properties.DocumentGroup);
    }
  }

  partial class SupAgreementCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      
      if (_source.LeadingDocument == null || !_source.LeadingDocument.AccessRights.CanRead())
        e.Without(_info.Properties.LeadingDocument);
    }
  }

  partial class SupAgreementLeadingDocumentPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> LeadingDocumentFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = base.LeadingDocumentFiltering(query, e);
      
      if (_obj.Counterparty != null)
        query = query.Where(c => Equals(c.Counterparty, _obj.Counterparty));
      
      query = query.Where(c => !Equals(c.LifeCycleState, Sungero.Contracts.ContractBase.LifeCycleState.Obsolete) &&
                          !Equals(c.LifeCycleState, Sungero.Contracts.ContractBase.LifeCycleState.Closed) &&
                          !Equals(c.LifeCycleState, Sungero.Contracts.ContractBase.LifeCycleState.Terminated));

      // В процессе верификации при смене типа с договора на доп. соглашение
      // сущность может быть еще договором (до первого сохранения после смены),
      // и в этом случае не нужно предоставлять ее для выбора в качестве ведущего документа.
      query = query.Where(c => c.Id != _obj.Id);

      return query;
    }
  }

  partial class SupAgreementServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      if (_obj.State.IsInserted && _obj.LeadingDocument != null)
        _obj.Relations.AddFrom(Constants.Module.SupAgreementRelationName, _obj.LeadingDocument);
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Проверить неизменность ведущего документа, если нет прав на его изменение.
      var contractIsChanged = _obj.LeadingDocument != _obj.State.Properties.LeadingDocument.OriginalValue;
      if (contractIsChanged)
      {
        var isContractDisabled = Sungero.Docflow.PublicFunctions.OfficialDocument.NeedDisableLeadingDocument(_obj);
        if (isContractDisabled)
          e.AddError(Sungero.Docflow.OfficialDocuments.Resources.RelationPropertyDisabled);
      }
      
      if (_obj.ValidFrom > _obj.ValidTill)
      {
        e.AddError(_obj.Info.Properties.ValidFrom, SupAgreements.Resources.IncorrectValidDates, _obj.Info.Properties.ValidTill);
        e.AddError(_obj.Info.Properties.ValidTill, SupAgreements.Resources.IncorrectValidDates, _obj.Info.Properties.ValidFrom);
      }
      
      base.BeforeSave(e);
      
      if (_obj.LeadingDocument != null && _obj.LeadingDocument.AccessRights.CanRead() &&
          !_obj.Relations.GetRelatedFrom(Constants.Module.SupAgreementRelationName).Contains(_obj.LeadingDocument))
        _obj.Relations.AddFromOrUpdate(Constants.Module.SupAgreementRelationName, _obj.State.Properties.LeadingDocument.OriginalValue, _obj.LeadingDocument);
      
      if (Functions.SupAgreement.HaveDuplicates(_obj,
                                                _obj.BusinessUnit,
                                                _obj.RegistrationNumber,
                                                _obj.RegistrationDate,
                                                _obj.Counterparty,
                                                _obj.LeadingDocument))
        e.AddWarning(ContractualDocuments.Resources.DuplicatesDetected, _obj.Info.Actions.ShowDuplicates);
    }
  }

}