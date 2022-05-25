using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Parties.Structures.Module
{

  /// <summary>
  /// Информация о контрагенте, полученная из сервиса обмена + статус обмена.
  /// </summary>
  partial class CounterpartyFromExchangeService
  {
   public string Name { get; set; }
   
   public string TIN { get; set; }
   
   public string TRRC { get; set; }
   
   public ExchangeCore.IBusinessUnitBox Box { get; set; }
   
   public ICounterparty Counterparty { get; set; }
   
   public Sungero.Core.Enumeration? ExchangeStatus { get; set; }
   
   public string OrganizationId { get; set; }
  }

}