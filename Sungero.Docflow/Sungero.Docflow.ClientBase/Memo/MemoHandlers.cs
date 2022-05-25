using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Memo;

namespace Sungero.Docflow
{
  partial class MemoAddresseesClientHandlers
  {

    public virtual void AddresseesNumberValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue < 1)
        e.AddError(Sungero.Docflow.OfficialDocuments.Resources.NumberAddresseeListIsNotPositive);
    }
  }

  partial class MemoClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      Functions.Memo.ChangeAddresseePropertiesAccess(_obj);
    }
    
    public override IEnumerable<Enumeration> ExecutionStateFiltering(IEnumerable<Enumeration> query)
    {
      query = base.ExecutionStateFiltering(query);
      return query.Where(s => s != OfficialDocument.ExecutionState.OnReview);
    }
  }

}