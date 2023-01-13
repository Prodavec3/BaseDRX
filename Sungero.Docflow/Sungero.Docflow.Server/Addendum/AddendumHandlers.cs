using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Addendum;

namespace Sungero.Docflow
{
  partial class AddendumCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      
      if (_source.LeadingDocument == null || !_source.LeadingDocument.AccessRights.CanRead())
        e.Without(_info.Properties.LeadingDocument);
    }
  }

  partial class AddendumLeadingDocumentPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> LeadingDocumentFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = base.LeadingDocumentFiltering(query, e);
      // Исключить из выбора сам документ, а также черновики из системы обмена.
      return query.Where(d => !Equals(d, _obj) && !ExchangeDocuments.Is(d));
    }
  }

  partial class AddendumServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var leadingDocumentChanged = !Equals(_obj.LeadingDocument, _obj.State.Properties.LeadingDocument.OriginalValue);
      if (leadingDocumentChanged)
      {
        // Проверить, доступен ли для изменения ведущий документ.
        var isLeadingDocumentDisabled = Sungero.Docflow.PublicFunctions.OfficialDocument.NeedDisableLeadingDocument(_obj);
        if (isLeadingDocumentDisabled)
          e.AddError(Sungero.Docflow.OfficialDocuments.Resources.RelationPropertyDisabled);
        
        if (Functions.OfficialDocument.IsProjectDocument(_obj.LeadingDocument, new List<int>()))
          e.Params.AddOrUpdate(Constants.OfficialDocument.GrantAccessRightsToProjectDocument, true);
      }
      
      base.BeforeSave(e);
      
      if (_obj.LeadingDocument != null && leadingDocumentChanged && _obj.AccessRights.StrictMode != AccessRightsStrictMode.Enhanced)
      {
        var accessRightsLimit = Functions.OfficialDocument.GetAvailableAccessRights(_obj);
        if (accessRightsLimit != Guid.Empty)
          Functions.OfficialDocument.CopyAccessRightsToDocument(_obj.LeadingDocument, _obj, accessRightsLimit);
      }
      
      if (_obj.LeadingDocument != null && _obj.LeadingDocument.AccessRights.CanRead() &&
          !_obj.Relations.GetRelatedFrom(Constants.Module.AddendumRelationName).Contains(_obj.LeadingDocument))
        _obj.Relations.AddFromOrUpdate(Constants.Module.AddendumRelationName, _obj.State.Properties.LeadingDocument.OriginalValue, _obj.LeadingDocument);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      var isRemainObsoleteAfterTypeChange = e.Params.Contains(string.Format("doc{0}_ConvertingFrom", _obj.Id)) &&
        _obj.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Obsolete;
      
      base.Created(e);
      
      if (_obj.State.IsInserted && _obj.LeadingDocument != null)
        _obj.Relations.AddFrom(Constants.Module.AddendumRelationName, _obj.LeadingDocument);
      
      if (!isRemainObsoleteAfterTypeChange)
        _obj.LifeCycleState = Docflow.OfficialDocument.LifeCycleState.Active;
    }
  }

}