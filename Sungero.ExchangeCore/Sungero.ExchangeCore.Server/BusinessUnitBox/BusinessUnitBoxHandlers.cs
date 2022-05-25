using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.BusinessUnitBox;

namespace Sungero.ExchangeCore
{
  partial class BusinessUnitBoxCertificateReceiptNotificationsPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CertificateReceiptNotificationsFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var exchangeBoxCertificates = _obj.ExchangeServiceCertificates.Select(c => c.Certificate).ToList();
      return query.Where(x => x.Enabled == true && exchangeBoxCertificates.Contains(x));
    }
  }

  partial class BusinessUnitBoxExchangeServicePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ExchangeServiceFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Status == Sungero.CoreEntities.DatabookEntry.Status.Active && _obj.BusinessUnit != null)
      {
        var alreadyUsedServices = Functions.BusinessUnitBox.GetUsedServicesOfBox(_obj, _obj.BusinessUnit);
        if (alreadyUsedServices.Any())
          query = query
            .Where(x => !alreadyUsedServices.Contains(x));
      }
      
      return query;
    }
  }

  partial class BusinessUnitBoxServerHandlers
  {

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      base.AfterSave(e);
      if (!e.Params.Contains(Constants.BoxBase.JobRunned))
        Jobs.SyncBoxes.Enqueue();
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      _obj.ConnectionStatus = ConnectionStatus.Waiting;
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      Functions.BusinessUnitBox.SetBusinessUnitBoxName(_obj);
      Functions.BusinessUnitBox.CheckConnection(_obj);

      var errors = Functions.BusinessUnitBox.BeforeSaveCheckProperties(_obj);
      foreach (var error in errors)
        e.AddError(error.Key, error.Value);

      if (_obj.Status == Sungero.CoreEntities.DatabookEntry.Status.Active)
      {
        if (!Functions.BusinessUnitBox.CheckAllResponsibleCertificates(_obj, _obj.Responsible))
          e.AddWarning(_obj.Info.Properties.Responsible, BusinessUnitBoxes.Resources.CertificateNotFound, _obj.Info.Properties.Responsible);
        
        if (!Functions.BusinessUnitBox.CheckBusinessUnitTinTRRC(_obj))
          e.AddWarning(BusinessUnitBoxes.Resources.OrganizationFailed);
      }
    }
  }

  partial class BusinessUnitBoxCreatingFromServerHandler
  {
    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      
      e.Without(_info.Properties.Login);
      e.Without(_info.Properties.Password);
      e.Without(_info.Properties.OrganizationId);
      e.Without(_info.Properties.ExchangeService);
      e.Without(_info.Properties.FtsId);
      e.Without(_info.Properties.ExchangeServiceCertificates);
      e.Without(_info.Properties.HasExchangeServiceCertificates);
      e.Without(_info.Properties.CertificateReceiptNotifications);
    }
  }
}