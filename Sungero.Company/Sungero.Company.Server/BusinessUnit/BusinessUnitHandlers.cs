using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Company.BusinessUnit;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{
  partial class BusinessUnitUiFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.UiFilteringEventArgs e)
    {
      query = base.Filtering(query, e);
      if (Functions.Module.IsRecipientRestrict())
      {
        var visibleRecipientIds = Functions.Module.GetVisibleRecipientIds(Constants.Module.BusinessUnitTypeGuid);
        return query.Where(c => visibleRecipientIds.Contains(c.Id));
      }
      return query;
    }
  }

  partial class BusinessUnitHeadCompanyPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> HeadCompanyFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = query.Where(b => !Equals(b, _obj));
      
      return query;
    }
  }

  partial class BusinessUnitCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      // Отменить заполнение свойства организации для правильной синхронизации.
      e.Without(_source.Info.Properties.Company);
    }
  }

  partial class BusinessUnitServerHandlers
  {

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      // Запуск индексации, если Elasticsearch сконфигурирован и изменились индексируемые поля.
      if (Commons.PublicFunctions.Module.IsElasticsearchConfigured() &&
          e.Params.Contains(Sungero.Commons.PublicConstants.Module.IsIndexedEntityInsertedParamKey))
      {
        var allowCreateRecord = false;
        e.Params.TryGetValue(Sungero.Commons.PublicConstants.Module.IsIndexedEntityInsertedParamKey, out allowCreateRecord);
        e.Params.Remove(Sungero.Commons.PublicConstants.Module.IsIndexedEntityInsertedParamKey);
        Sungero.Commons.PublicFunctions.Module.CreateIndexEntityAsyncHandler(BusinessUnits.Info.Name, _obj.Id, Functions.BusinessUnit.GetIndexingJson(_obj), allowCreateRecord);
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.Nonresident = false;
    }

    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      #region Синхронизировать свойства нашей организации с организацией

      // Создать организацию, соответствующую нашей организации.
      var company = _obj.Company;
      if (company == null)
      {
        company = Parties.Companies.Create();
        _obj.Company = company;
      }

      company.Nonresident = _obj.Nonresident;
      company.Name = _obj.Name;
      company.TIN = _obj.TIN;
      company.TRRC = _obj.TRRC;
      company.PSRN = _obj.PSRN;
      company.NCEO = _obj.NCEO;
      company.NCEA = _obj.NCEA;
      company.City = _obj.City;
      company.Phones = _obj.Phones;
      company.LegalName = _obj.LegalName;
      company.Region = _obj.Region;
      company.LegalAddress = _obj.LegalAddress;
      company.PostalAddress = _obj.PostalAddress;
      company.Status = _obj.Status;
      company.Email = _obj.Email;
      company.Homepage = _obj.Homepage;
      company.Account = _obj.Account;
      company.Bank = _obj.Bank;
      company.Code = _obj.Code;

      if (_obj.HeadCompany != null)
        company.HeadCompany = _obj.HeadCompany.Company;
      else
        company.HeadCompany = null;

      company.Note = string.Format("{0}{1}", BusinessUnits.Resources.BusinessUnitComment, _obj.Note);
      company.IsCardReadOnly = true;
      
      #endregion
      
      // Синхронизировать с ролью "Руководители наших организаций".
      Functions.BusinessUnit.SynchronizeCEOInRole(_obj);
      
      // Создать или обновить права подписи у руководителя.
      Functions.BusinessUnit.UpdateSignatureSettings(_obj);
    }

    public override void Deleting(Sungero.Domain.DeletingEventArgs e)
    {
      var managers = Departments.GetAll()
        .Where(d => d.BusinessUnit.Equals(_obj) && d.HeadOffice == null)
        .Select(d => d.Manager)
        .Where(m => m != null)
        .ToList();
      
      // Удаление замещений руководителя.
      if (_obj.CEO != null)
        Sungero.Company.Functions.Module.DeleteSystemSubstitutions(managers, _obj.CEO);
    }

    public override void Saved(Sungero.Domain.SavedEventArgs e)
    {
      var managers = Departments.GetAll()
        .Where(d => d.BusinessUnit.Equals(_obj) && d.HeadOffice == null)
        .Select(d => d.Manager)
        .Where(m => m != null)
        .ToList();
      
      if (!managers.Any())
        return;
      
      if (_obj.State.Properties.CEO.IsChanged &&
          _obj.State.Properties.CEO.OriginalValue != null)
        Functions.Module.DeleteSystemSubstitutions(managers, _obj.State.Properties.CEO.OriginalValue);
      
      if (_obj.CEO != null)
      {
        if (_obj.State.Properties.CEO.IsChanged ||
            (_obj.State.Properties.Status.IsChanged &&
             _obj.Status == Sungero.CoreEntities.DatabookEntry.Status.Active))
          Functions.Module.CreateSystemSubstitutions(managers, _obj.CEO);
        
        if (_obj.State.Properties.Status.IsChanged &&
            _obj.Status == Sungero.CoreEntities.DatabookEntry.Status.Closed)
          Functions.Module.DeleteSystemSubstitutions(managers, _obj.CEO);
      }
    }

    public override void AfterDelete(Sungero.Domain.AfterDeleteEventArgs e)
    {
      // Удалить организацию, соответствующую нашей организации.
      if (_obj.Company != null)
      {
        _obj.Company.IsCardReadOnly = false;
        Parties.CompanyBases.Delete(_obj.Company);
      }
      
      // Удаление из индекса Elasticsearch, если он сконфигурирован.
      if (Commons.PublicFunctions.Module.IsElasticsearchConfigured())
        Commons.PublicFunctions.Module.CreateRemoveEntityFromIndexAsyncHandler(BusinessUnits.Info.Name, _obj.Id);
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (!string.IsNullOrEmpty(_obj.TRRC))
        _obj.TRRC = _obj.TRRC.Trim();
      
      if (!string.IsNullOrEmpty(_obj.TIN))
        _obj.TIN = _obj.TIN.Trim();
      
      if (!string.IsNullOrEmpty(_obj.PSRN))
        _obj.PSRN = _obj.PSRN.Trim();
      
      if (!string.IsNullOrEmpty(_obj.NCEO))
        _obj.NCEO = _obj.NCEO.Trim();
      
      // Проверить код на пробелы, если свойство изменено.
      if (!string.IsNullOrEmpty(_obj.Code))
      {
        // При изменении кода e.AddError сбрасывается.
        var codeIsChanged = _obj.State.Properties.Code.IsChanged;
        _obj.Code = _obj.Code.Trim();
        
        if (codeIsChanged && Regex.IsMatch(_obj.Code, @"\s"))
          e.AddError(_obj.Info.Properties.Code, Company.Resources.NoSpacesInCode);
      }
      
      #region Проверить корректность цифровых кодов и дубли
      
      if (_obj.Nonresident != true)
      {
        // Проверить корректность ИНН.
        var checkTinErrorText = Parties.PublicFunctions.Counterparty.CheckTin(_obj.TIN, true);
        if (!string.IsNullOrEmpty(checkTinErrorText))
          e.AddError(_obj.Info.Properties.TIN, checkTinErrorText);
        
        // Проверить корректность КПП.
        var checkTrrcErrorText = Parties.PublicFunctions.CompanyBase.CheckTRRC(_obj.TRRC);
        if (!string.IsNullOrEmpty(checkTrrcErrorText))
          e.AddError(_obj.Info.Properties.TRRC, checkTrrcErrorText);

        // Проверить корректность ОГРН.
        var checkPsrnErrorText = Functions.BusinessUnit.CheckPsrnLength(_obj, _obj.PSRN);
        if (!string.IsNullOrEmpty(checkPsrnErrorText))
          e.AddError(_obj.Info.Properties.PSRN, checkPsrnErrorText);
        
        // Проверить корректность ОКПО.
        var checkNceoErrorText = Functions.BusinessUnit.CheckNceoLength(_obj, _obj.NCEO);
        if (!string.IsNullOrEmpty(checkNceoErrorText))
          e.AddError(_obj.Info.Properties.NCEO, checkNceoErrorText);
      }

      var checkDuplicatesErrorText = Functions.BusinessUnit.GetCounterpartyDuplicatesErrorText(_obj);
      if (!string.IsNullOrEmpty(checkDuplicatesErrorText))
        e.AddWarning(checkDuplicatesErrorText, _obj.Info.Actions.ShowDuplicates);

      #endregion
      
      #region Проверить циклические ссылки в подчиненных НОР
      
      if (_obj.State.Properties.HeadCompany.IsChanged && _obj.HeadCompany != null)
      {
        var headCompany = _obj.HeadCompany;
        
        while (headCompany != null)
        {
          if (Equals(headCompany, _obj))
          {
            e.AddError(_obj.Info.Properties.HeadCompany, BusinessUnits.Resources.HeadCompanyCyclicReference, _obj.Info.Properties.HeadCompany);
            break;
          }
          
          headCompany = headCompany.HeadCompany;
        }
      }
      
      #endregion
      
      // Выставить параметр необходимости индексации сущности, при изменении индексируемых полей.
      var props = _obj.State.Properties;
      if (props.Name.IsChanged || props.LegalName.IsChanged || props.TIN.IsChanged || props.TRRC.IsChanged || props.PSRN.IsChanged || props.Status.IsChanged)
        e.Params.AddOrUpdate(Sungero.Commons.PublicConstants.Module.IsIndexedEntityInsertedParamKey, _obj.State.IsInserted);
    }
  }
}