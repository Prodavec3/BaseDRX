using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DistributionList;

namespace Sungero.Docflow
{
  partial class DistributionListAddresseesAddresseePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> AddresseesAddresseeFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Correspondent != null)
      {
        if (Sungero.Parties.People.Is(_obj.Correspondent))
          return query.Where(c => c.Company == null);
        
        query = query.Where(c => Equals(c.Company, _obj.Correspondent));
      }
      return query;
    }
  }

}