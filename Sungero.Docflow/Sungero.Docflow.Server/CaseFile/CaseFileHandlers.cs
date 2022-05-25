using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.CaseFile;

namespace Sungero.Docflow
{

  partial class CaseFileRegistrationGroupPropertyFilteringServerHandler<T>
  {
    public virtual IQueryable<T> RegistrationGroupFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var clerks = Functions.DocumentRegister.GetClerks();
      if (clerks == null)
        return query;
      var allRecipientIds = Recipients.AllRecipientIds;
      if (allRecipientIds.Contains(clerks.Id))
        query = query.Where(g => g.RecipientLinks.Any(l => allRecipientIds.Contains(l.Member.Id)));
      
      return query;
    }
  }

  partial class CaseFileFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      var clerks = Functions.DocumentRegister.GetClerks();
      if (clerks != null)
      {
        var allRecipientIds = Recipients.AllRecipientIds;
        if (allRecipientIds.Contains(clerks.Id))
          query = query.Where(f => f.RegistrationGroup == RegistrationGroups.Null || f.RegistrationGroup.RecipientLinks.Any(l => allRecipientIds.Contains(l.Member.Id)));
      }
      
      var filter = _filter;
      if (filter == null)
        return query;
      
      // Фильтр по состоянию.
      if (filter.Active || filter.Closed)
        query = query.Where(f => (filter.Active && f.Status.Value == CoreEntities.DatabookEntry.Status.Active) ||
                            (filter.Closed && f.Status.Value == CoreEntities.DatabookEntry.Status.Closed));
      
      // Фильтр по НОР.
      if (filter.BusinessUnit != null)
      {
        var departmentIds = Company.PublicFunctions.BusinessUnit.Remote.GetAllDepartmentIds(filter.BusinessUnit);
        query = query.Where(f => f.BusinessUnit != null && Equals(f.BusinessUnit, filter.BusinessUnit) ||
                            f.BusinessUnit == null && f.Department != null && departmentIds.Contains(f.Department.Id));
      }
      
      // Фильтр по подразделению.
      if (filter.Department != null)
        query = query.Where(f => f.Department == filter.Department);
      
      // Фильтр по признаку "Переходящее".
      if (filter.Transient)
        query = query.Where(f => f.LongTerm.HasValue && f.LongTerm.Value);
      
      var currentYear = Calendar.UserToday.Year;
      
      // Фильтр по текущему году.
      if (filter.CurrentYear)
        query = query.Where(f => f.StartDate.Value.Year <= currentYear && (!f.EndDate.HasValue || f.EndDate.Value.Year >= currentYear));
      
      // Фильтр по предыдущему году.
      var previousYear = currentYear - 1;
      if (filter.PreviousYear)
        query = query.Where(f => f.StartDate.Value.Year <= previousYear && (!f.EndDate.HasValue || f.EndDate.Value.Year >= previousYear));
      
      // Фильтр по следующему году.
      var nextYear = currentYear + 1;
      if (filter.NextYear)
        query = query.Where(f => f.StartDate.Value.Year <= nextYear && (!f.EndDate.HasValue || f.EndDate.Value.Year >= nextYear));
      
      // Фильтр по диапазону лет.
      if (filter.ManualPeriod)
      {
        if (filter.DateRangeTo != null)
          query = query.Where(f => f.StartDate.Value.Year <= filter.DateRangeTo.Value.Year);
        if (filter.DateRangeFrom != null)
          query = query.Where(f => !f.EndDate.HasValue || f.EndDate.Value.Year >= filter.DateRangeFrom.Value.Year);
      }
      
      return query;
    }
  }

  partial class CaseFileServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      // Заполнить свойство "переходящее" по умолчанию.
      _obj.LongTerm = false;
      
      if (_obj.BusinessUnit == null)
        _obj.BusinessUnit = Functions.Module.GetDefaultBusinessUnit(Company.Employees.Current);
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Проверить правильность заполнения дат.
      if (_obj.StartDate > _obj.EndDate)
      {
        e.AddError(_obj.Info.Properties.StartDate, CaseFiles.Resources.WrongPeriod, new[] { _obj.Info.Properties.StartDate, _obj.Info.Properties.EndDate });
        e.AddError(_obj.Info.Properties.EndDate, CaseFiles.Resources.WrongPeriod, new[] { _obj.Info.Properties.StartDate, _obj.Info.Properties.EndDate });
      }
      
      // Проверить уникальность индекса в рамках нашей организации и периода.
      if (!Functions.CaseFile.CheckIndexForUniqueness(_obj))
        e.AddError(CaseFiles.Resources.IndexIsNotUnique);
      
      // Проверить индекс на пробелы, если свойство изменено.
      if (!string.IsNullOrEmpty(_obj.Index))
      {
        // При изменении индекса e.AddError сбрасывается.
        var indexIsChanged = _obj.State.Properties.Index.IsChanged;
        _obj.Index = _obj.Index.Trim();
        if (indexIsChanged && Regex.IsMatch(_obj.Index, @"\s"))
          e.AddError(_obj.Info.Properties.Index, Docflow.CaseFiles.Resources.NoSpacesInIndex);
      }
      
      _obj.Name = string.Format("{0}. {1}", _obj.Index, _obj.Title);
    }
  }
}