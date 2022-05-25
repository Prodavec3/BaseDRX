using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ContractCategory;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts
{
  partial class ContractCategoryDocumentKindsDocumentKindPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> DocumentKindsDocumentKindFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var availableDocumentKinds = Functions.ContractCategory.GetAllowedDocumentKinds();

      query = base.DocumentKindsDocumentKindFiltering(query, e);
      return query.Where(a => availableDocumentKinds.Contains(a));
    }
  }

}