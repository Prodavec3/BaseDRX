using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Projects.Server
{

  partial class ProjectDocumentsFolderHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> ProjectDocumentsDataQuery(IQueryable<Sungero.Docflow.IOfficialDocument> query)
    {
      query = query.Where(d => d.Project != null);
      
      if (_filter == null)
        return query;
      
      if (_filter.Project != null)
        query = query.Where(d => Equals(d.Project, _filter.Project));
      
      if (_filter.DocumentKind != null)
        query = query.Where(d => Equals(d.DocumentKind, _filter.DocumentKind));
      
      if (_filter.Author != null)
        query = query.Where(d => Equals(d.Author, _filter.Author));
      
      // Фильтр по интервалу времени.
      var periodBegin = Calendar.SqlMinValue;
      var periodEnd = Calendar.UserToday.EndOfDay().FromUserTime();
      
      if (_filter.LastWeek)
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-7));
      
      if (_filter.LastMonth)
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-30));
      
      if (_filter.Last90Days)
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-90));
      
      if (_filter.ManualPeriod)
      {
        if (_filter.ChangedFrom != null)
          periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(_filter.ChangedFrom.Value);
        
        periodEnd = _filter.ChangedTo != null ? _filter.ChangedTo.Value.EndOfDay().FromUserTime() : Calendar.SqlMaxValue;
      }
      
      return query.Where(d => d.Modified.Between(periodBegin, periodEnd));
    }

    public virtual IQueryable<Sungero.Docflow.IDocumentKind> ProjectDocumentsDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      query = query.Where(dk => dk.Status == CoreEntities.DatabookEntry.Status.Active && dk.ProjectsAccounting == true);
      return query;
    }
  }

  partial class ProjectsHandlers
  {
  }
}