using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DistributionList;

namespace Sungero.Docflow
{
  partial class DistributionListAddresseesSharedCollectionHandlers
  {

    public virtual void AddresseesAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      _added.Number = (_obj.Addressees.Max(a => a.Number) ?? 0) + 1;
    }
  }

  partial class DistributionListAddresseesSharedHandlers
  {

    public virtual void AddresseesAddresseeChanged(Sungero.Docflow.Shared.DistributionListAddresseesAddresseeChangedEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue) && !Equals(e.NewValue.Company, _obj.Correspondent))
        _obj.Correspondent = e.NewValue.Company;
    }

    public virtual void AddresseesCorrespondentChanged(Sungero.Docflow.Shared.DistributionListAddresseesCorrespondentChangedEventArgs e)
    {
      if (!Equals(e.NewValue, e.OldValue))
      {
        if (_obj.Addressee != null && !Equals(_obj.Addressee.Company, e.NewValue))
          _obj.Addressee = null;
      }
    }
  }

  partial class DistributionListSharedHandlers
  {

  }
}