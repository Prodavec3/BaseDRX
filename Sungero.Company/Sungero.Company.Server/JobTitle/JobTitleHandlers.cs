using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.JobTitle;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{
  partial class JobTitleFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return query;
      
      if (_filter.Active || _filter.Closed)
        query = query.Where(r => _filter.Active && r.Status == Status.Active ||
                            _filter.Closed && r.Status == Status.Closed);
      
      if (_filter.Department != null)
      {
        if (!_filter.IncludeSubdepartments)
          query = query.Where(r => Equals(r.Department, _filter.Department));
        else
        {
          var subDepartmentIds = PublicFunctions.Department.Remote.GetSubordinateDepartmentIds(_filter.Department);
          query = query.Where(r => Equals(r.Department, _filter.Department) ||
                              subDepartmentIds.Contains(r.Department.Id));
        }
      }
      
      if (_filter.BusinessUnit != null)
      {
        var allBusinessUnitDepartments = Functions.BusinessUnit.GetAllDepartments(_filter.BusinessUnit);
        query = query.Where(r => allBusinessUnitDepartments.Contains(r.Department));
      }
      
      return query;
    }
  }

  partial class JobTitleServerHandlers
  {
    
    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      _obj.Name = _obj.Name.Trim();
      if (string.IsNullOrWhiteSpace(_obj.Name))
        e.AddError(_obj.Info.Properties.Name, Commons.Resources.RequiredPropertiesNotFilledIn);
      
      var duplicates = Functions.JobTitle.GetDuplicates(_obj);
      if (duplicates.Any())
        e.AddError(JobTitles.Resources.JobTitleNameAlreadyExists, _obj.Info.Actions.ShowDuplicates);
      
      var employees = Functions.JobTitle.GetEmployeesWithSameJobTitle(_obj);
      if (_obj.Department != null && employees.Any())
        e.AddError(Sungero.Company.JobTitles.Resources.JobTitleAlreadyAssignedToEmployees, _obj.Info.Actions.ShowEmployeesWithSameJobTitle);
    }
  }
}