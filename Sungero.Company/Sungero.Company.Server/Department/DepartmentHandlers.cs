using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Company;
using Sungero.Company.Department;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{
  partial class DepartmentUiFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.UiFilteringEventArgs e)
    {
      query = base.Filtering(query, e);
      if (Functions.Module.IsRecipientRestrict())
        query = Functions.Department.RestrictDepartments(query).Cast<T>();
      
      return query;
    }
  }

  partial class DepartmentCreatingFromServerHandler
  {
    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      // Отменить заполнение сотрудников.
      e.Without(_source.Info.Properties.RecipientLinks);
    }
  }

  partial class DepartmentHeadOfficePropertyFilteringServerHandler<T>
  {
    public virtual IQueryable<T> HeadOfficeFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      // Фильтровать головное подразделение по нашим организациям.
      if (_obj.BusinessUnit != null)
        query = query.Where(d => d.BusinessUnit.Equals(_obj.BusinessUnit));
      
      return query.Where(d => !Equals(d, _obj));
    }
  }

  partial class DepartmentServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // При исключении работника из подразделения проверить, что оно для него не основное.
      foreach (var departmentEmployee in Employees.GetAll(x => Equals(x.Department, _obj)))
      {
        // TODO Zamerov нужен нормальный признак IsDeleted, 50908
        var isDeleted = (departmentEmployee as Sungero.Domain.Shared.IChangeTracking).ChangeTracker.IsDeleted;
        if (!isDeleted && !_obj.RecipientLinks.Any(l => Equals(l.Member, departmentEmployee)))
        {
          e.AddError(Departments.Resources.YouСantDeleteEmployeeOflastDivision);
          return;
        }
      }
      
      // Проверить код подразделения на пробелы, если свойство изменено.
      if (!string.IsNullOrEmpty(_obj.Code))
      {
        // При изменении кода e.AddError сбрасывается.
        var codeIsChanged = _obj.State.Properties.Code.IsChanged;
        _obj.Code = _obj.Code.Trim();
        
        if (codeIsChanged && Regex.IsMatch(_obj.Code, @"\s"))
          e.AddError(_obj.Info.Properties.Code, Company.Resources.NoSpacesInCode);
      }
    }

    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      Functions.Department.SynchronizeManagerInRole(_obj);
      // Создать системное замещение.
      var systemSubstitutionsForCreate = new List<Structures.Module.Substitution>();
      var systemSubstitutionsForDelete = new List<Structures.Module.Substitution>();
      
      if (_obj.State.IsInserted)
      {
        this.InitSystemSubstitutions(systemSubstitutionsForCreate);
        this.CommitSystemSubstitutions(systemSubstitutionsForCreate, systemSubstitutionsForDelete);
        
        return;
      }
      
      this.UpdateSystemSubstitutionForChangedManager(systemSubstitutionsForCreate, systemSubstitutionsForDelete);
      this.UpdateSystemSubstitutionForChangedDepartmentStructure(systemSubstitutionsForCreate, systemSubstitutionsForDelete);
      this.UpdateSystemSubstitutionForChangedHeadOffice(systemSubstitutionsForCreate, systemSubstitutionsForDelete);
      this.UpdateSystemSubstitutionForChangedBusinessUnit(systemSubstitutionsForCreate, systemSubstitutionsForDelete);
      this.UpdateSystemSubstitutionForChangedStatus(systemSubstitutionsForCreate, systemSubstitutionsForDelete);
      this.CommitSystemSubstitutions(systemSubstitutionsForCreate, systemSubstitutionsForDelete);
    }

    public override void Deleting(Sungero.Domain.DeletingEventArgs e)
    {
      if (_obj.Manager == null)
        return;
      
      if (_obj.RecipientLinks.Any())
      {
        var members = _obj.RecipientLinks.Select(r => r.Member).ToList().Select(m => Users.As(m)).Where(m => m != null).ToList();
        Functions.Module.DeleteSystemSubstitutions(members, _obj.Manager);
      }
      
      if (_obj.HeadOffice != null && _obj.HeadOffice.Manager != null)
        Functions.Module.DeleteSystemSubstitutions(new[] { _obj.Manager }, _obj.HeadOffice.Manager);
      
      if (_obj.HeadOffice == null && _obj.BusinessUnit != null && _obj.BusinessUnit.CEO != null)
        Functions.Module.DeleteSystemSubstitutions(new[] { _obj.Manager }, _obj.BusinessUnit.CEO);
      Functions.Department.SynchronizeManagerInRole(_obj);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      var activeBusinessUnits = Company.BusinessUnits.GetAll().Where(oc => oc.Status == CoreEntities.DatabookEntry.Status.Active);
      
      if (activeBusinessUnits.Count() == 1)
        _obj.BusinessUnit = activeBusinessUnits.FirstOrDefault();
    }

    #region Работа с замещениями

    /// <summary>
    /// Отработка изменения системных замещений при изменении руководителя подразделения.
    /// </summary>
    /// <param name="systemSubstitutionsForCreate">Создаваемые замещения.</param>
    /// <param name="systemSubstitutionsForDelete">Удаляемые замещения.</param>
    private void UpdateSystemSubstitutionForChangedManager(List<Structures.Module.Substitution> systemSubstitutionsForCreate,
                                                           List<Structures.Module.Substitution> systemSubstitutionsForDelete)
    {
      if (_obj.State.Properties.Manager.IsChanged)
      {
        if (_obj.RecipientLinks.Any())
        {
          var members = _obj.RecipientLinks.Select(r => r.Member).ToList().Select(m => Users.As(m)).Where(m => m != null).ToList();
          
          // Удаление замещений существующего руководителя.
          if (_obj.State.Properties.Manager.OriginalValue != null)
            this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                     _obj.State.Properties.Manager.OriginalValue,
                                                     members);
          
          // Создание замещений для нового руководителя.
          if (_obj.Manager != null)
            this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                     _obj.Manager,
                                                     members);
        }
        
        if (_obj.Manager != null)
        {
          // Создание/удаление замещений на руководителей дочерних подразделений.
          var childDepartmentManagers = Departments.GetAll().Where(d => d.HeadOffice.Equals(_obj))
            .Select(d => d.Manager).Where(m => m != null).ToList();
          
          if (childDepartmentManagers.Any())
          {
            if (_obj.State.Properties.Manager.OriginalValue != null)
              this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                       _obj.State.Properties.Manager.OriginalValue,
                                                       childDepartmentManagers);
            
            this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                     _obj.Manager,
                                                     childDepartmentManagers);
          }
          
          // Создание/удаление замещения руководителя головного подразделения.
          if (_obj.HeadOffice != null && _obj.HeadOffice.Manager != null)
          {
            if (_obj.State.Properties.Manager.OriginalValue != null)
              this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                       _obj.HeadOffice.Manager,
                                                       new[] { _obj.State.Properties.Manager.OriginalValue });
            
            this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                     _obj.HeadOffice.Manager,
                                                     new[] { _obj.Manager });
          }
          
          // Создание/удаление замещения руководителя НОР.
          if (_obj.BusinessUnit != null && _obj.BusinessUnit.CEO != null)
          {
            if (_obj.State.Properties.Manager.OriginalValue != null)
              this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                       _obj.BusinessUnit.CEO,
                                                       new[] { _obj.State.Properties.Manager.OriginalValue });
            
            this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                     _obj.BusinessUnit.CEO,
                                                     new[] { _obj.Manager });
          }
        }
        
        // Удаление замещений руководителей.
        if (_obj.Manager == null && _obj.State.Properties.Manager.OriginalValue != null)
        {
          // Удаление замещений на руководителей дочерних подразделений.
          var childDepartmentManagers = Departments.GetAll().Where(d => d.HeadOffice.Equals(_obj))
            .Select(d => d.Manager).Where(m => m != null).ToList();
          
          if (childDepartmentManagers.Any())
            this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                     _obj.State.Properties.Manager.OriginalValue,
                                                     childDepartmentManagers);
          
          // Удаление замещения руководителя головного подразделения.
          if (_obj.HeadOffice != null && _obj.HeadOffice.Manager != null)
            this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                     _obj.HeadOffice.Manager,
                                                     new[] { _obj.State.Properties.Manager.OriginalValue });
          
          // Удаление замещения руководителя НОР.
          if (_obj.HeadOffice == null && _obj.BusinessUnit != null && _obj.BusinessUnit.CEO != null)
            this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                     _obj.BusinessUnit.CEO,
                                                     new[] { _obj.State.Properties.Manager.OriginalValue });
        }
      }
    }

    /// <summary>
    /// Отработка изменения системных замещений при изменении состава подразделения.
    /// </summary>
    /// <param name="systemSubstitutionsForCreate">Создаваемые замещения.</param>
    /// <param name="systemSubstitutionsForDelete">Удаляемые замещения.</param>
    private void UpdateSystemSubstitutionForChangedDepartmentStructure(List<Structures.Module.Substitution> systemSubstitutionsForCreate,
                                                                       List<Structures.Module.Substitution> systemSubstitutionsForDelete)
    {
      if (_obj.State.Properties.RecipientLinks.IsChanged && _obj.Manager != null)
      {
        if (_obj.State.Properties.RecipientLinks.Deleted.Any())
        {
          var deletedMembers = _obj.State.Properties.RecipientLinks.Deleted.Select(r => r.Member).ToList().Select(m => Users.As(m))
            .Where(m => m != null).ToList();
          
          this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                   _obj.Manager,
                                                   deletedMembers);
        }
        
        if (_obj.State.Properties.RecipientLinks.Added.Any())
        {
          var addedMembers = _obj.State.Properties.RecipientLinks.Added.Select(r => r.Member).ToList().Select(m => Users.As(m))
            .Where(m => m != null).ToList();
          
          this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                   _obj.Manager,
                                                   addedMembers);
        }
        
        var changedRecipientLinks = _obj.State.Properties.RecipientLinks.Changed.Where(r => !r.State.IsInserted).ToList();
        
        if (changedRecipientLinks.Any())
        {
          var deletedMembers = changedRecipientLinks.Select(r => r.State.Properties.Member.OriginalValue).ToList().Select(m => Users.As(m))
            .Where(m => m != null).ToList();
          var addedMembers = changedRecipientLinks.Select(r => r.Member).ToList().Select(m => Users.As(m))
            .Where(m => m != null).ToList();
          
          this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                   _obj.Manager,
                                                   deletedMembers);
          this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                   _obj.Manager,
                                                   addedMembers);
        }
      }
    }

    /// <summary>
    /// Отработка изменения системных замещений при изменении головного подразделения.
    /// </summary>
    /// <param name="systemSubstitutionsForCreate">Создаваемые замещения.</param>
    /// <param name="systemSubstitutionsForDelete">Удаляемые замещения.</param>
    private void UpdateSystemSubstitutionForChangedHeadOffice(List<Structures.Module.Substitution> systemSubstitutionsForCreate,
                                                              List<Structures.Module.Substitution> systemSubstitutionsForDelete)
    {
      // Изменение головного подразделения.
      if (_obj.State.Properties.HeadOffice.IsChanged && _obj.Manager != null)
      {
        // Удаление замещений руководителя старого головного подразделения.
        if (_obj.State.Properties.HeadOffice.OriginalValue != null && _obj.State.Properties.HeadOffice.OriginalValue.Manager != null)
          this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                   _obj.State.Properties.HeadOffice.OriginalValue.Manager,
                                                   new[] { _obj.Manager });
        
        // Создание замещений для руководителя нового головного подразделения.
        if (_obj.HeadOffice != null && _obj.HeadOffice.Manager != null)
          this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                   _obj.HeadOffice.Manager,
                                                   new[] { _obj.Manager });
        
        // Создание замещений для руководителя НОР.
        if (_obj.HeadOffice == null && _obj.BusinessUnit != null && _obj.BusinessUnit.CEO != null)
          this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                   _obj.BusinessUnit.CEO,
                                                   new[] { _obj.Manager });
      }
    }

    /// <summary>
    /// Отработка изменения системных замещений при изменении НОР.
    /// </summary>
    /// <param name="systemSubstitutionsForCreate">Создаваемые замещения.</param>
    /// <param name="systemSubstitutionsForDelete">Удаляемые замещения.</param>
    private void UpdateSystemSubstitutionForChangedBusinessUnit(List<Structures.Module.Substitution> systemSubstitutionsForCreate,
                                                                List<Structures.Module.Substitution> systemSubstitutionsForDelete)
    {
      // Изменение НОР.
      if (_obj.State.Properties.BusinessUnit.IsChanged && _obj.HeadOffice == null && _obj.Manager != null)
      {
        // Удаление замещений руководителя старой НОР.
        if (_obj.State.Properties.BusinessUnit.OriginalValue != null && _obj.State.Properties.BusinessUnit.OriginalValue.CEO != null)
          this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                   _obj.State.Properties.BusinessUnit.OriginalValue.CEO,
                                                   new[] { _obj.Manager });
        
        // Создание замещений для руководителя новой НОР.
        if (_obj.BusinessUnit != null && _obj.BusinessUnit.CEO != null)
          this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                   _obj.BusinessUnit.CEO,
                                                   new[] { _obj.Manager });
      }
    }

    /// <summary>
    /// Отработка изменения системных замещений при изменении состояния подразделения.
    /// </summary>
    /// <param name="systemSubstitutionsForCreate">Создаваемые замещения.</param>
    /// <param name="systemSubstitutionsForDelete">Удаляемые замещения.</param>
    private void UpdateSystemSubstitutionForChangedStatus(List<Structures.Module.Substitution> systemSubstitutionsForCreate,
                                                          List<Structures.Module.Substitution> systemSubstitutionsForDelete)
    {
      if (_obj.State.Properties.Status.IsChanged)
      {
        if (_obj.Status == Sungero.CoreEntities.DatabookEntry.Status.Active)
          this.InitSystemSubstitutions(systemSubstitutionsForCreate);
        
        if (_obj.Status == Sungero.CoreEntities.DatabookEntry.Status.Closed)
          this.ClearSystemSubstitutions(systemSubstitutionsForDelete);
      }
    }

    /// <summary>
    /// Инициализировать первоначальные системные замещения.
    /// </summary>
    /// <param name="systemSubstitutionsForCreate">Создаваемые замещения.</param>
    private void InitSystemSubstitutions(List<Structures.Module.Substitution> systemSubstitutionsForCreate)
    {
      if (_obj.Manager != null)
      {
        // Создание замещений руководитель - состав подразделения.
        if (_obj.RecipientLinks.Any())
        {
          var members = _obj.RecipientLinks.Select(r => r.Member).ToList().Select(m => Users.As(m)).Where(m => m != null).ToList();
          
          this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                   _obj.Manager,
                                                   members);
        }
        
        // Создание замещений на руководителей дочерних подразделений.
        var childDepartmentManagers = Departments.GetAll().Where(d => d.HeadOffice.Equals(_obj)).Select(d => d.Manager)
          .Where(m => m != null).ToList();
        
        if (childDepartmentManagers.Any())
          this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                   _obj.Manager,
                                                   childDepartmentManagers);
        
        // Создание замещения руководителя головной организации.
        if (_obj.HeadOffice != null && _obj.HeadOffice.Manager != null)
          this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                   _obj.HeadOffice.Manager,
                                                   new[] { _obj.Manager });
        
        // Создание замещения руководителя НОР.
        if (_obj.HeadOffice == null && _obj.BusinessUnit != null && _obj.BusinessUnit.CEO != null)
          this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                   _obj.BusinessUnit.CEO,
                                                   new[] { _obj.Manager });
      }
    }

    /// <summary>
    /// Зачистить системные замещения.
    /// </summary>
    /// <param name="systemSubstitutionsForDelete">Удаляемые замещения.</param>
    private void ClearSystemSubstitutions(List<Structures.Module.Substitution> systemSubstitutionsForDelete)
    {
      if (_obj.Manager != null)
      {
        // Удаление замещений руководитель - состав подразделения.
        if (_obj.RecipientLinks.Any())
        {
          var members = _obj.RecipientLinks.Select(r => r.Member).ToList().Select(m => Users.As(m)).Where(m => m != null).ToList();
          
          this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                   _obj.Manager,
                                                   members);
        }
        
        // Удаление замещений на руководителей дочерних подразделений.
        var childDepartmentManagers = Departments.GetAll().Where(d => d.HeadOffice.Equals(_obj)).Select(d => d.Manager)
          .Where(m => m != null).ToList();
        
        if (childDepartmentManagers.Any())
          this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                   _obj.Manager,
                                                   childDepartmentManagers);
        
        // Удаление замещения руководителя головной организации.
        if (_obj.HeadOffice != null && _obj.HeadOffice.Manager != null)
          this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                   _obj.HeadOffice.Manager,
                                                   new[] { _obj.Manager });
        
        // Удаление замещения руководителя НОР.
        if (_obj.HeadOffice == null && _obj.BusinessUnit != null && _obj.BusinessUnit.CEO != null)
          this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                   _obj.BusinessUnit.CEO,
                                                   new[] { _obj.Manager });
      }
    }

    /// <summary>
    /// Обновление списка системных замещений.
    /// </summary>
    /// <param name="currentSystemSubstitutions">Текущий список системных замещений.</param>
    /// <param name="user">Замещающий.</param>
    /// <param name="substitutedUsers">Замещаемый пользователь.</param>
    private void UpdateSystemSubstitutionsCollection(List<Structures.Module.Substitution> currentSystemSubstitutions,
                                                     IUser user, IEnumerable<IUser> substitutedUsers)
    {
      foreach (var substituted in substitutedUsers)
        currentSystemSubstitutions.Add(Structures.Module.Substitution.Create(user, substituted));
    }

    /// <summary>
    /// Сохранить изменения системных замещений.
    /// </summary>
    /// <param name="systemSubstitutionsForCreate">Список для создания системных замещений.</param>
    /// <param name="systemSubstitutionsForDelete">Список для удаления системных замещений.</param>
    private void CommitSystemSubstitutions(IEnumerable<Structures.Module.Substitution> systemSubstitutionsForCreate,
                                           IEnumerable<Structures.Module.Substitution> systemSubstitutionsForDelete)
    {
      foreach (var element in systemSubstitutionsForCreate.Distinct())
        Sungero.Company.Functions.Module.CreateSystemSubstitution(element.SubstitutedUser, element.User);
      
      foreach (var element in systemSubstitutionsForDelete.Distinct())
        Sungero.Company.Functions.Module.DeleteSystemSubstitution(element.SubstitutedUser, element.User);
    }

    #endregion
  }
}