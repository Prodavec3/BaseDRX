using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PowerOfAttorneyBase;

namespace Sungero.Docflow
{
  partial class PowerOfAttorneyBaseFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      #region Фильтры
      
      if (_filter == null)
        return query;
      
      // Состояние.
      if ((_filter.DraftState || _filter.ActiveState || _filter.ObsoleteState) &&
          !(_filter.DraftState && _filter.ActiveState && _filter.ObsoleteState))
      {
        query = query.Where(c => (_filter.DraftState && c.LifeCycleState == AccountingDocumentBase.LifeCycleState.Draft) ||
                            (_filter.ActiveState && c.LifeCycleState == AccountingDocumentBase.LifeCycleState.Active) ||
                            (_filter.ObsoleteState && c.LifeCycleState == AccountingDocumentBase.LifeCycleState.Obsolete));
      }
      
      // Фильтр "Наша организация".
      if (_filter.BusinessUnit != null)
        query = query.Where(c => Equals(c.BusinessUnit, _filter.BusinessUnit));
      
      // Фильтр "Подразделение".
      if (_filter.Department != null)
        query = query.Where(c => Equals(c.Department, _filter.Department));
      
      // Фильтр "Кому выдана".
      if (_filter.Performer != null)
        query = query.Where(c => Equals(c.IssuedTo, _filter.Performer));
      
      // Период.
      if (_filter.Today)
        query = query.Where(c => (!c.RegistrationDate.HasValue || c.RegistrationDate <= Calendar.UserToday) &&
                            (!c.ValidTill.HasValue || c.ValidTill >= Calendar.UserToday));
      
      if (_filter.ManualPeriodPOA)
      {
        if (_filter.DateRangeFrom.HasValue)
          query = query.Where(c => !c.ValidTill.HasValue || c.ValidTill >= _filter.DateRangeFrom);
        if (_filter.DateRangeTo.HasValue)
          query = query.Where(c => !c.RegistrationDate.HasValue || c.RegistrationDate <= _filter.DateRangeTo);
      }
      
      #endregion
      
      return query;
    }
  }

  partial class PowerOfAttorneyBaseCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      e.Without(_info.Properties.ValidTill);
    }
  }

  partial class PowerOfAttorneyBaseServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      // Очистить поля Подразделение и НОР, заполненные в предке.
      if (!_obj.State.IsCopied)
      {
        _obj.Department = null;
        _obj.BusinessUnit = null;
      }
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      if (_obj.ValidFrom > _obj.ValidTill)
      {
        e.AddError(_obj.Info.Properties.ValidFrom, PowerOfAttorneyBases.Resources.IncorrectValidDates, _obj.Info.Properties.ValidTill);
        e.AddError(_obj.Info.Properties.ValidTill, PowerOfAttorneyBases.Resources.IncorrectValidDates, _obj.Info.Properties.ValidFrom);
      }
      
      // При изменении сотрудника в "Кому выдана" проверить наличие действующих прав подписи.
      if (!Equals(_obj.IssuedTo, _obj.State.Properties.IssuedTo.OriginalValue))
      {
        var signSettings = Functions.PowerOfAttorneyBase.GetActiveSignatureSettingsByPOA(_obj);
        if (signSettings.Any())
          e.AddError(PowerOfAttorneyBases.Resources.AlreadyExistSignatureSetting, _obj.Info.Actions.FindActiveSignatureSetting);
      }
      
      // Выдать права на документ сотруднику, указанному в поле "Кому выдана".
      if (_obj.IssuedTo != null && _obj.AccessRights.StrictMode != AccessRightsStrictMode.Enhanced)
        _obj.AccessRights.Grant(_obj.IssuedTo, DefaultAccessRightsTypes.Read);
    }
  }

}