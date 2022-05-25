using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Bank;

namespace Sungero.Parties
{
  partial class BankServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (!string.IsNullOrEmpty(_obj.SWIFT))
      {
        var newSwift = _obj.SWIFT.Trim();
        if (!_obj.SWIFT.Equals(newSwift, StringComparison.InvariantCultureIgnoreCase))
          _obj.SWIFT = newSwift;
      }
      
      if (!string.IsNullOrEmpty(_obj.BIC))
      {
        var newBic = _obj.BIC.Trim();
        if (!_obj.BIC.Equals(newBic, StringComparison.InvariantCultureIgnoreCase))
          _obj.BIC = newBic;
      }
      
      if (!string.IsNullOrEmpty(_obj.CorrespondentAccount))
      {
        var newCorr = _obj.CorrespondentAccount.Trim();
        if (!_obj.CorrespondentAccount.Equals(newCorr, StringComparison.InvariantCultureIgnoreCase))
          _obj.CorrespondentAccount = newCorr;
      }
      
      // Проверить корректность SWIFT.
      var checkSwiftErrorText = PublicFunctions.Bank.CheckSwift(_obj.SWIFT);
      if (!string.IsNullOrEmpty(checkSwiftErrorText))
        e.AddError(_obj.Info.Properties.SWIFT, checkSwiftErrorText);
      
      if (_obj.Nonresident != true)
      {
        // Проверить корректность БИК.
        var checkBicErrorText = PublicFunctions.Bank.CheckBicLength(_obj.BIC);
        if (!string.IsNullOrEmpty(checkBicErrorText))
          e.AddError(_obj.Info.Properties.BIC, checkBicErrorText);
        
        // Проверить корректность корр. счета.
        var checkCorrErrorText = PublicFunctions.Bank.CheckCorrLength(_obj.CorrespondentAccount);
        if (!string.IsNullOrEmpty(checkCorrErrorText))
          e.AddError(_obj.Info.Properties.CorrespondentAccount, checkCorrErrorText);
      }
      else
      {
        // Проверить корректность корр. счета для нерезидента.
        var checkCorrErrorText = PublicFunctions.Bank.CheckCorrAccountForNonresident(_obj.CorrespondentAccount);
        if (!string.IsNullOrEmpty(checkCorrErrorText))
          e.AddError(_obj.Info.Properties.CorrespondentAccount, checkCorrErrorText);
      }
      
      base.BeforeSave(e);
    }
  }

  partial class BankCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      e.Without(_info.Properties.IsSystem);
      
      // При копировании системного банка комментарий не переносится.
      if (_source.IsSystem == true)
        e.Without(_info.Properties.Note);
    }
  }

}