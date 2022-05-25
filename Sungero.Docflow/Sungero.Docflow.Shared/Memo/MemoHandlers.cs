using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Memo;

namespace Sungero.Docflow
{
  partial class MemoSharedHandlers
  {

    public virtual void AddresseeChanged(Sungero.Docflow.Shared.MemoAddresseeChangedEventArgs e)
    {
      if (_obj.IsManyAddressees == false)
        Functions.Memo.ClearAndFillFirstAddressee(_obj);
    }

    public virtual void IsManyAddresseesChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue == true)
      {
        Functions.Memo.ClearAndFillFirstAddressee(_obj);
        Functions.Memo.SetManyAddresseesPlaceholder(_obj);
      }
      else if (e.NewValue == false)
      {
        Functions.Memo.FillAddresseeFromAddressees(_obj);
        Functions.Memo.ClearAndFillFirstAddressee(_obj);
      }
    }
  }

  partial class MemoAddresseesSharedHandlers
  {

    public virtual void AddresseesAddresseeChanged(Sungero.Docflow.Shared.MemoAddresseesAddresseeChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue.Department != null)
        _obj.Department = e.NewValue.Department;
    }
  }

  partial class MemoAddresseesSharedCollectionHandlers
  {

    public virtual void AddresseesAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      _added.Number = (_obj.Addressees.Max(a => a.Number) ?? 0) + 1;
    }
  }

}