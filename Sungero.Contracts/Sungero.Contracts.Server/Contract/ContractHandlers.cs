using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.Contract;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;

namespace Sungero.Contracts
{
  partial class ContractConvertingFromServerHandler
  {
    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);
      
      e.Without(Sungero.Contracts.SupAgreements.Info.Properties.LeadingDocument);
    }
  }
}