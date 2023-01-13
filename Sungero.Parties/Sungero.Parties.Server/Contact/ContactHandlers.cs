using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Contact;

namespace Sungero.Parties
{
  partial class ContactServerHandlers
  {

    public override void AfterDelete(Sungero.Domain.AfterDeleteEventArgs e)
    {
      // Удаление из индекса Elasticsearch, если он сконфигурирован.
      if (Commons.PublicFunctions.Module.IsElasticsearchConfigured())
        Commons.PublicFunctions.Module.CreateRemoveEntityFromIndexAsyncHandler(Contacts.Info.Name, _obj.Id);
    }

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      // Запуск индексации, если Elasticsearch сконфигурирован и изменились индексируемые поля.
      if (Commons.PublicFunctions.Module.IsElasticsearchConfigured() && e.Params.Contains(Sungero.Commons.PublicConstants.Module.IsIndexedEntityInsertedParamKey))
      {
        var allowCreateRecord = false;
        e.Params.TryGetValue(Sungero.Commons.PublicConstants.Module.IsIndexedEntityInsertedParamKey, out allowCreateRecord);
        e.Params.Remove(Sungero.Commons.PublicConstants.Module.IsIndexedEntityInsertedParamKey);
        Sungero.Commons.PublicFunctions.Module.CreateIndexEntityAsyncHandler(Contacts.Info.Name, _obj.Id, Functions.Contact.GetIndexingJson(_obj), allowCreateRecord);
      }
    }
    
    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (Functions.Contact.HaveDuplicates(_obj))
        e.AddWarning(Sungero.Commons.Resources.DuplicateDetected, _obj.Info.Actions.ShowDuplicates);
      
      // Выставить параметр необходимости индексации сущности, при изменении индексируемых полей.
      var props = _obj.State.Properties;
      if (props.Name.IsChanged || props.Company.IsChanged || props.Status.IsChanged)
        e.Params.AddOrUpdate(Sungero.Commons.PublicConstants.Module.IsIndexedEntityInsertedParamKey, _obj.State.IsInserted);
    }

    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      if (!Commons.PublicFunctions.Module.IsAllExternalEntityLinksDeleted(_obj))
        throw AppliedCodeException.Create(Commons.Resources.HasLinkedExternalEntities);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      ICompanyBase company;
      if (CallContext.CalledFrom(CompanyBases.Info))
      {
        company = CompanyBases.Get(CallContext.GetCallerEntityId(CompanyBases.Info));
        _obj.Company = company;
      }
      
      if (Contacts.Info.Properties.Company.TryGetFilter(out company))
      {
        _obj.Company = company;
      }
    }
  }
}