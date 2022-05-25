using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Memo;

namespace Sungero.Docflow
{
  partial class MemoAddresseesDepartmentPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> AddresseesDepartmentFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Addressee == null)
        return query;
      
      return query.Where(x => x.RecipientLinks.Any(r => Equals(r.Member, _obj.Addressee)));
    }
  }

  partial class MemoOurSignatoryPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> OurSignatoryFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query;
    }
  }

  partial class MemoServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);

      // Заполнить адресата на главной первым адресатом из коллекции для отображения в списке.
      if (_obj.IsManyAddressees == true)
        Functions.Memo.FillAddresseeFromAddressees(_obj);
      
      Functions.Memo.SetManyAddresseesLabel(_obj);

      var addresseesLimit = Functions.Memo.GetAddresseesLimit(_obj);
      if (_obj.Addressees.Count > addresseesLimit)
        e.AddError(Memos.Resources.TooManyAddresseesFormat(addresseesLimit));
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      if (_obj.IsManyAddressees == null)
        _obj.IsManyAddressees = false;
      
      _obj.State.Properties.ManyAddresseesPlaceholder.IsEnabled = false;
      
      // Заполнить "Подписал".
      var employee = Company.Employees.Current;
      if (_obj.OurSignatory == null)
        _obj.OurSignatory = employee;
    }
  }
}