using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ContractualDocument;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts
{
  partial class ContractualDocumentCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      e.Without(_info.Properties.Milestones);
    }
  }

  partial class ContractualDocumentCounterpartySignatoryPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> CounterpartySignatoryFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = base.CounterpartySignatoryFiltering(query, e);
      
      if (_obj.Counterparty != null)
        query = query.Where(c => c.Company == _obj.Counterparty);
      
      return query;
    }
  }

  partial class ContractualDocumentServerHandlers
  {

    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      if (!Commons.PublicFunctions.Module.IsAllExternalEntityLinksDeleted(_obj))
        throw AppliedCodeException.Create(Commons.Resources.HasLinkedExternalEntities);
      
      base.BeforeDelete(e);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      _obj.IsStandard = false;
      
      if (_obj.ResponsibleEmployee == null)
        _obj.ResponsibleEmployee = Company.Employees.As(_obj.Author);
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      // Выдать ответственному права на изменение документа.
      var responsible = _obj.ResponsibleEmployee;
      if (responsible != null && !Equals(_obj.State.Properties.ResponsibleEmployee.OriginalValue, responsible) &&
          !Equals(responsible, Sungero.Company.Employees.Current) &&
          !_obj.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, responsible) &&
          !_obj.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, responsible) &&
          _obj.AccessRights.StrictMode != AccessRightsStrictMode.Enhanced)
        _obj.AccessRights.Grant(responsible, DefaultAccessRightsTypes.Change);
    }
  }

  partial class ContractualDocumentContactPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ContactFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Counterparty != null)
        query = query.Where(c => c.Company == _obj.Counterparty);
      
      return query;
    }
  }

}