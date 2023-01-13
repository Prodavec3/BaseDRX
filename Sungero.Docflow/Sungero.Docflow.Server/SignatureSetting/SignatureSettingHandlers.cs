using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.SignatureSetting;
using Sungero.Domain.Shared;

namespace Sungero.Docflow
{
  partial class SignatureSettingCertificatePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CertificateFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(c => c.Enabled == true && Equals(_obj.Recipient, c.Owner) && (!c.NotAfter.HasValue || c.NotAfter >= Calendar.Now));
    }
  }

  partial class SignatureSettingCategoriesCategoryPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CategoriesCategoryFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return Functions.SignatureSetting.FilterCategories(_root, query).Cast<T>();
    }
  }

  partial class SignatureSettingDocumentKindsDocumentKindPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> DocumentKindsDocumentKindFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_root.DocumentFlow != null && _root.DocumentFlow != SignatureSetting.DocumentFlow.All)
        query = query.Where(d => d.DocumentFlow == _root.DocumentFlow);
      return query;
    }
  }

  partial class SignatureSettingDocumentPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> DocumentFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = query.Where(d => InternalDocumentBases.Is(d) && d.LifeCycleState != Docflow.OfficialDocument.LifeCycleState.Obsolete);
      
      if (_obj.Reason == Docflow.SignatureSetting.Reason.PowerOfAttorney)
      {
        query = query.Where(d => PowerOfAttorneys.Is(d) &&
                            PowerOfAttorneys.As(d).ValidTill >= Calendar.UserToday &&
                            d.LifeCycleState != Docflow.OfficialDocument.LifeCycleState.Draft);

        if (_obj.Recipient != null)
          query = query.Where(d => Equals(_obj.Recipient, PowerOfAttorneys.As(d).IssuedTo));
      }
      
      if (_obj.Reason == Docflow.SignatureSetting.Reason.FormalizedPoA)
      {
        query = query.Where(d => FormalizedPowerOfAttorneys.Is(d) &&
                            FormalizedPowerOfAttorneys.As(d).ValidTill >= Calendar.UserToday &&
                            d.LifeCycleState != Docflow.OfficialDocument.LifeCycleState.Draft);

        if (_obj.Recipient != null)
          query = query.Where(d => Equals(_obj.Recipient, FormalizedPowerOfAttorneys.As(d).IssuedTo));
      }
      
      return query;
    }
  }

  partial class SignatureSettingRecipientPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> RecipientFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var signatureRoles = Functions.SignatureSetting.GetPossibleSignatureRoles(_obj);
      return query.Where(r => (Roles.Is(r) && signatureRoles.Contains(r.Sid.Value)) || Employees.Is(r));
    }
  }

  partial class SignatureSettingFilteringServerHandler<T>
  {

    public virtual IQueryable<Sungero.Company.IEmployee> RecipientFiltering(IQueryable<Sungero.Company.IEmployee> query, Sungero.Domain.FilteringEventArgs e)
    {
      e.DisableUiFiltering = true;
      return query;
    }
    
    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return query;
      
      if (_filter.Active || _filter.Closed)
        query = query.Where(s => (_filter.Active && s.Status == CoreEntities.DatabookEntry.Status.Active) ||
                            (_filter.Closed && s.Status == CoreEntities.DatabookEntry.Status.Closed));
      
      if (_filter.DocumentKind != null)
        query = query.Where(s => !s.DocumentKinds.Any() || s.DocumentKinds.Any(k => Equals(k.DocumentKind, _filter.DocumentKind)));
      
      if (_filter.Today)
        query = query.Where(s => (!s.ValidFrom.HasValue || s.ValidFrom.Value <= Calendar.UserToday) &&
                            (!s.ValidTill.HasValue || s.ValidTill.Value >= Calendar.UserToday));
      if (_filter.Period)
      {
        if (_filter.DateRangeFrom.HasValue)
          query = query.Where(s => !s.ValidTill.HasValue || s.ValidTill.Value >= _filter.DateRangeFrom.Value);
        if (_filter.DateRangeTo.HasValue)
          query = query.Where(s => !s.ValidFrom.HasValue || s.ValidFrom.Value <= _filter.DateRangeTo.Value);
      }
      
      if (_filter.Recipient != null)
      {
        var ids = Recipients.OwnRecipientIdsFor(_filter.Recipient).ToList();
        query = query.Where(s => ids.Contains(s.Recipient.Id));
      }
      
      if (_filter.Incoming || _filter.Outgoing || _filter.Inner || _filter.Contracts)
        query = query.Where(s => (s.DocumentFlow == SignatureSetting.DocumentFlow.Incoming && _filter.Incoming) ||
                            (s.DocumentFlow == SignatureSetting.DocumentFlow.Outgoing && _filter.Outgoing) ||
                            (s.DocumentFlow == SignatureSetting.DocumentFlow.Inner && _filter.Inner) ||
                            (s.DocumentFlow == SignatureSetting.DocumentFlow.Contracts && _filter.Contracts) ||
                            s.DocumentFlow == SignatureSetting.DocumentFlow.All);
      
      return query;
    }
  }

  partial class SignatureSettingServerHandlers
  {

    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      Functions.SignatureSetting.UpdateSigningRole(_obj, false);
    }

    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      Functions.SignatureSetting.UpdateSigningRole(_obj, true);
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.ValidFrom > _obj.ValidTill)
      {
        e.AddError(_obj.Info.Properties.ValidFrom, SignatureSettings.Resources.IncorrectValidDates, _obj.Info.Properties.ValidTill);
        e.AddError(_obj.Info.Properties.ValidTill, SignatureSettings.Resources.IncorrectValidDates, _obj.Info.Properties.ValidFrom);
      }
      
      if (_obj.Reason == SignatureSetting.Reason.PowerOfAttorney)
      {
        if (!Docflow.PowerOfAttorneys.Is(_obj.Document))
          e.AddError(SignatureSettings.Info.Properties.Document, Docflow.SignatureSettings.Resources.IncorrectDocumentType);
        else if (_obj.ValidTill > Docflow.PowerOfAttorneys.As(_obj.Document).ValidTill)
          e.AddError(Docflow.SignatureSettings.Resources.IncorrectValidTill);
      }
      
      if (_obj.Reason == SignatureSetting.Reason.FormalizedPoA)
      {
        // В праве подписи на основании электронной доверенности нужно указать ровно одну НОР.
        if (_obj.BusinessUnits.Count > 1)
          e.AddError(SignatureSettings.Info.Properties.BusinessUnits, SignatureSettings.Resources.FormalizedPoATooManyBusinessUnits);
        
        var powerOfAttorney = FormalizedPowerOfAttorneys.As(_obj.Document);
        
        var settingValidFrom = _obj.ValidFrom.HasValue ? _obj.ValidFrom.Value : Calendar.Today;
        // Дата начала действия доверенности - либо Действует с, либо Дата регистрации, либо дата создания.
        DateTime powerOfAttorneyValidFrom;
        if (powerOfAttorney.ValidFrom.HasValue)
          powerOfAttorneyValidFrom = powerOfAttorney.ValidFrom.Value;
        else if (powerOfAttorney.RegistrationDate.HasValue)
          powerOfAttorneyValidFrom = powerOfAttorney.RegistrationDate.Value;
        else
          powerOfAttorneyValidFrom = powerOfAttorney.Created.Value;
        
        if (settingValidFrom < powerOfAttorneyValidFrom || _obj.ValidTill > powerOfAttorney.ValidTill)
        {
          e.AddError(_obj.Info.Properties.ValidFrom, SignatureSettings.Resources.SettingValidityPeriodShouldNotExtendDocumentValidityPeriod, _obj.Info.Properties.ValidTill);
          e.AddError(_obj.Info.Properties.ValidTill, SignatureSettings.Resources.SettingValidityPeriodShouldNotExtendDocumentValidityPeriod, _obj.Info.Properties.ValidFrom);
        }
        
        if (!_obj.BusinessUnits.Select(x => x.BusinessUnit).Contains(powerOfAttorney.BusinessUnit))
        {
          e.AddError(_obj.Info.Properties.Document, SignatureSettings.Resources.SignatoryDataDoesNotMatchWithDocument, _obj.Info.Properties.BusinessUnits);
          e.AddError(_obj.Info.Properties.BusinessUnits, SignatureSettings.Resources.SignatoryDataDoesNotMatchWithDocument, _obj.Info.Properties.Document);
        }
        
        if (!Equals(powerOfAttorney.IssuedTo, _obj.Recipient))
        {
          e.AddError(_obj.Info.Properties.Document, SignatureSettings.Resources.SignatoryDataDoesNotMatchWithDocument, _obj.Info.Properties.Recipient);
          e.AddError(_obj.Info.Properties.Recipient, SignatureSettings.Resources.SignatoryDataDoesNotMatchWithDocument, _obj.Info.Properties.Document);
        }
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      if (!_obj.State.IsCopied)
      {
        _obj.Limit = Limit.NoLimit;
        
        if (CallContext.CalledFrom(PowerOfAttorneys.Info))
        {
          _obj.Reason = Sungero.Docflow.SignatureSetting.Reason.PowerOfAttorney;
          var powerOfAttorney = PowerOfAttorneyBases.Get(CallContext.GetCallerEntityId(PowerOfAttorneys.Info));
          if (powerOfAttorney.LifeCycleState != Docflow.OfficialDocument.LifeCycleState.Obsolete &&
              powerOfAttorney.LifeCycleState != Docflow.OfficialDocument.LifeCycleState.Draft &&
              powerOfAttorney.ValidTill >= Calendar.UserToday)
          {
            _obj.Document = powerOfAttorney;
          }
        }
        else if (CallContext.CalledFrom(FormalizedPowerOfAttorneys.Info))
        {
          _obj.Reason = Sungero.Docflow.SignatureSetting.Reason.FormalizedPoA;
          var formalizedPowerOfAttorney = PowerOfAttorneyBases.Get(CallContext.GetCallerEntityId(FormalizedPowerOfAttorneys.Info));
          if (formalizedPowerOfAttorney.LifeCycleState != Docflow.OfficialDocument.LifeCycleState.Obsolete &&
              formalizedPowerOfAttorney.LifeCycleState != Docflow.OfficialDocument.LifeCycleState.Draft &&
              formalizedPowerOfAttorney.ValidTill >= Calendar.UserToday)
          {
            _obj.Document = formalizedPowerOfAttorney;
          }
        }
        else
          _obj.Reason = Sungero.Docflow.SignatureSetting.Reason.Duties;
        
        _obj.DocumentFlow = SignatureSetting.DocumentFlow.All;
        _obj.Priority = 0;
      }
      
      _obj.IsSystem = false;
    }
  }

}