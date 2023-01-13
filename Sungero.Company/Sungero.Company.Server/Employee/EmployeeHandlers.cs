using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.Employee;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{

  partial class EmployeeJobTitlePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> JobTitleFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Department != null)
        return query.Where(x => Equals(x.Department, _obj.Department) || x.Department == null);
      return query;
    }
  }

  partial class EmployeeUiFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.UiFilteringEventArgs e)
    {
      query = base.Filtering(query, e);
      if (Functions.Module.IsRecipientRestrict())
      {
        var visibleRecipientIds = Functions.Module.GetVisibleRecipientIds(Constants.Module.EmployeeTypeGuid);
        return query.Where(c => visibleRecipientIds.Contains(c.Id));
      }
      return query;
    }
  }

  partial class EmployeeFilteringServerHandler<T>
  {

    public virtual IQueryable<Sungero.Company.IJobTitle> JobTitleFiltering(IQueryable<Sungero.Company.IJobTitle> query, Sungero.Domain.FilteringEventArgs e)
    {
      return query.Where(r => Equals(r.Status, Status.Active));
    }

    public virtual IQueryable<Sungero.Company.IDepartment> DepartmentFiltering(IQueryable<Sungero.Company.IDepartment> query, Sungero.Domain.FilteringEventArgs e)
    {
      return query.Where(r => Equals(r.Status, Status.Active));
    }

    public virtual IQueryable<Sungero.Company.IBusinessUnit> BusinessUnitFiltering(IQueryable<Sungero.Company.IBusinessUnit> query, Sungero.Domain.FilteringEventArgs e)
    {
      return query.Where(r => Equals(r.Status, Status.Active));
    }

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return query;
      
      if (_filter.Active || _filter.Closed)
        query = query.Where(r => _filter.Active && r.Status == Status.Active ||
                            _filter.Closed && r.Status == Status.Closed);

      if (_filter.JobTitle != null)
        query = query.Where(r => Equals(r.JobTitle, _filter.JobTitle));
      
      if (_filter.Department != null)
      {
        var employees = Departments.GetAllUsersInGroup(_filter.Department).ToList();
        query = query.Where(r => employees.Contains(r));
      }
      
      if (_filter.BusinessUnit != null)
      {
        var employees = Company.BusinessUnits.GetAllUsersInGroup(_filter.BusinessUnit).ToList();
        query = query.Where(r => employees.Contains(r));
      }
      
      return query;
    }
  }

  partial class EmployeeCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.NeedNotifyNewAssignments);
      e.Without(_info.Properties.NeedNotifyExpiredAssignments);
    }
  }

  partial class EmployeeServerHandlers
  {

    public override void AfterDelete(Sungero.Domain.AfterDeleteEventArgs e)
    {
      // Удаление из индекса Elasticsearch, если он сконфигурирован.
      if (Commons.PublicFunctions.Module.IsElasticsearchConfigured())
        Commons.PublicFunctions.Module.CreateRemoveEntityFromIndexAsyncHandler(Employees.Info.Name, _obj.Id);
    }

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      // Запуск индексации, если Elasticsearch сконфигурирован и изменились индексируемые поля .
      if (Commons.PublicFunctions.Module.IsElasticsearchConfigured() && e.Params.Contains(Sungero.Commons.PublicConstants.Module.IsIndexedEntityInsertedParamKey))
      {
        var allowCreateRecord = false;
        e.Params.TryGetValue(Sungero.Commons.PublicConstants.Module.IsIndexedEntityInsertedParamKey, out allowCreateRecord);
        e.Params.Remove(Sungero.Commons.PublicConstants.Module.IsIndexedEntityInsertedParamKey);
        Sungero.Commons.PublicFunctions.Module.CreateIndexEntityAsyncHandler(Employees.Info.Name, _obj.Id, Functions.Employee.GetIndexingJson(_obj), allowCreateRecord);
      }
    }

    public override IDigestModel GetDigest(Sungero.Domain.GetDigestEventArgs e)
    {
      return Sungero.Company.Functions.Module.GetEmployeePopup(_obj);
    }

    public override void Saved(Sungero.Domain.SavedEventArgs e)
    {
      var oldDepartment = _obj.State.Properties.Department.OriginalValue;
      var newDepartment = _obj.Department;
      
      if (!Equals(oldDepartment, newDepartment))
      {
        newDepartment.RecipientLinks.AddNew().Member = _obj;
        
        if (oldDepartment != null)
          foreach (var department in oldDepartment.RecipientLinks.Where(r => r.Member.Equals(_obj)).ToList())
            oldDepartment.RecipientLinks.Remove(department);
      }
    }

    public override void Deleting(Sungero.Domain.DeletingEventArgs e)
    {
      var systemSubstitutions = Substitutions.GetAll().Where(s => s.IsSystem == true && (s.Substitute.Equals(_obj) || s.User.Equals(_obj))).ToList();
      foreach (var substitution in systemSubstitutions)
        Substitutions.Delete(substitution);
    }
    
    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      var disableMailNotificationParam = Functions.Employee.GetDisableMailNotificationParam(_obj);
      _obj.NeedNotifyNewAssignments = !disableMailNotificationParam;
      _obj.NeedNotifyExpiredAssignments = !disableMailNotificationParam;
      _obj.NeedNotifyAssignmentsSummary = !disableMailNotificationParam;
    }

    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      _obj.Department.RecipientLinks.Remove(_obj.Department.RecipientLinks.Where(d => d.Member == _obj).FirstOrDefault());
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      Functions.Employee.UpdateName(_obj, _obj.Person);
      
      if (string.IsNullOrEmpty(_obj.Name))
        _obj.Name = " ";
      
      if (!Functions.Employee.IsValidEmail(_obj.Email))
        e.AddWarning(_obj.Info.Properties.Email, Parties.Resources.WrongEmailFormat);
      else if (!Docflow.PublicFunctions.Module.IsASCII(_obj.Email))
        e.AddWarning(_obj.Info.Properties.Email, Docflow.Resources.ASCIIWarning);
      
      var oldDepartment = _obj.State.Properties.Department.OriginalValue;
      var newDepartment = _obj.Department;
      
      if (!Equals(oldDepartment, newDepartment))
      {
        if (newDepartment != null)
        {
          var newDepartmentLockInfo = Locks.GetLockInfo(newDepartment);
          if (newDepartmentLockInfo.IsLockedByOther)
            e.AddError(Employees.Resources.DeparmentLockedByUserFormat(newDepartment.Name, newDepartmentLockInfo.OwnerName));
        }
        
        if (oldDepartment != null)
        {
          var oldDepartmentLockInfo = Locks.GetLockInfo(oldDepartment);
          if (oldDepartmentLockInfo.IsLockedByOther)
            e.AddError(Employees.Resources.DeparmentLockedByUserFormat(oldDepartment.Name, oldDepartmentLockInfo.OwnerName));
        }
      }
      
      // Выставить параметр необходимости индексации сущности, при изменении индексируемых полей.
      var props = _obj.State.Properties;
      if (props.Name.IsChanged || props.Person.IsChanged || props.Department.IsChanged || props.Status.IsChanged)
        e.Params.AddOrUpdate(Sungero.Commons.PublicConstants.Module.IsIndexedEntityInsertedParamKey, _obj.State.IsInserted);
    }
  }
}