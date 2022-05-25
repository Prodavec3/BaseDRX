using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Parties.Structures.Counterparty
{
  /// <summary>
  /// Информация о возможности отправки приглашений контрагенту в сервис обмена.
  /// </summary>
  partial class SendInvitation
  {
    public List<ExchangeCore.IBusinessUnitBox> Boxes { get; set; }
    
    public ExchangeCore.IBusinessUnitBox DefaultBox { get; set; }
    
    public bool HaveAllowedBoxes { get; set; }
    
    public bool HaveAnyBoxes { get; set; }
    
    public bool HaveDoubleCounterparty { get; set; }
    
    public List<ExchangeCore.IExchangeService> Services { get; set; }
    
    public bool CanSendInivtationFromAnyService { get; set; }
    
    public bool CanDoAction { get; set; }
  }
  
  partial class AllowedBoxes
  {
    public ExchangeCore.IBusinessUnitBox Box { get; set; }
    
    public List<string> OrganizationIds { get; set; }
  }
}