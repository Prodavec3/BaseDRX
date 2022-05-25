using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.FinancialArchive.UniversalTransferDocument;

namespace Sungero.FinancialArchive.Server
{
  partial class UniversalTransferDocumentFunctions
  {
    public override bool CanSendAnswer()
    {
      return _obj.IsFormalized == true ?
        Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetIncomingExDocumentInfo(_obj) != null :
        base.CanSendAnswer();
    }
    
    public override void SendAnswer(Sungero.ExchangeCore.IBusinessUnitBox box, Sungero.Parties.ICounterparty party, ICertificate certificate, bool isAgent)
    {
      if (_obj.IsFormalized == true)
      {
        Exchange.PublicFunctions.Module.SendBuyerTitle(_obj, box, certificate, isAgent);
      }
      else
      {
        base.SendAnswer(box, party, certificate, isAgent);
      }
    }
  }
}