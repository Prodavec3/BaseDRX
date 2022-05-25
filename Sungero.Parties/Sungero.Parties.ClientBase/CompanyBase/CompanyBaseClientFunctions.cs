using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.CompanyBase;

namespace Sungero.Parties.Client
{
  partial class CompanyBaseFunctions
  {
    public override bool ValidateTinTrrcBeforeExchange(Sungero.Domain.Client.ExecuteActionArgs args)
    {
      var based = base.ValidateTinTrrcBeforeExchange(args);
      if (based)
      {
        if (_obj.TIN != null && _obj.TIN.Length == 10 && string.IsNullOrWhiteSpace(_obj.TRRC))
        {
          if (args.Action.Name == _obj.Info.Actions.SendInvitation.Name)
          {
            args.AddError(CompanyBases.Resources.NeedFillTinAndTrrcForSendInvitation);
            based = false;
          }
          if (args.Action.Name == _obj.Info.Actions.CanExchange.Name)
          {
            args.AddError(CompanyBases.Resources.NeedFillTinAndTrrcForCanExchange);
            based = false;
          }
        }
      }
      return based;
    }
  }
}