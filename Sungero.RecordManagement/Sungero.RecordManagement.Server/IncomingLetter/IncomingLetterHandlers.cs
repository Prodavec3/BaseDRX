using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.IncomingLetter;

namespace Sungero.RecordManagement
{
  partial class IncomingLetterConvertingFromServerHandler
  {

    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);
      
      // Для Входящих документов эл. обмена мапим Контрагента в Корреспондента.
      if (Docflow.ExchangeDocuments.Is(_source))
        e.Map(_info.Properties.SignedBy, Docflow.ExchangeDocuments.Info.Properties.CounterpartySignatory);
    }
  }

  partial class IncomingLetterAddresseePropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> AddresseeFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = base.AddresseeFiltering(query, e);
      return query;
    }
  }

  partial class IncomingLetterContactPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ContactFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Correspondent != null)
        query = query.Where(c => Equals(c.Company, _obj.Correspondent));
      return query;
    }
  }

  partial class IncomingLetterSignedByPropertyFilteringServerHandler<T>
  {
    public virtual IQueryable<T> SignedByFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Correspondent != null)
        query = query.Where(c => Equals(c.Company, _obj.Correspondent));
      return query;
    }
  }

  partial class IncomingLetterServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      if (Functions.IncomingLetter.HaveDuplicates(_obj,
                                                  _obj.DocumentKind,
                                                  _obj.BusinessUnit,
                                                  _obj.InNumber,
                                                  _obj.Dated,
                                                  _obj.Correspondent))
        e.AddWarning(IncomingLetters.Resources.DuplicateDetected, _obj.Info.Actions.ShowDuplicates);
    }
  }

}