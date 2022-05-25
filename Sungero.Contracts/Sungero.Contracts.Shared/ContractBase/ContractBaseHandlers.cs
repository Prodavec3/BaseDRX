using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ContractBase;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;

namespace Sungero.Contracts
{
  partial class ContractBaseSharedHandlers
  {
    public virtual void DaysToFinishWorksChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      Functions.ContractBase.SetRequiredProperties(_obj);
    }

    public virtual void IsAutomaticRenewalChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      Functions.ContractBase.SetRequiredProperties(_obj);
    }

    public override void DocumentKindChanged(Sungero.Docflow.Shared.OfficialDocumentDocumentKindChangedEventArgs e)
    {
      base.DocumentKindChanged(e);
      
      var documentKind = e.NewValue;
      if (documentKind == null)
        return;
      
      // Очистить категорию, если она не соответствует виду документа.
      var availableCategory = Docflow.PublicFunctions.DocumentGroupBase.GetAvailableDocumentGroup(documentKind).Where(g => ContractCategories.Is(g));
      if (_obj.DocumentGroup != null && !availableCategory.Contains(_obj.DocumentGroup))
        _obj.DocumentGroup = null;
      
      // Заполнить категорию, если для выбранного вида доступна только одна.
      if (_obj.DocumentGroup == null && availableCategory.Count() == 1)
        _obj.DocumentGroup = availableCategory.First();
      
      // Проставить корректно жизненный цикл для договора после смены типа.
      if (_obj.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Obsolete)
      {
        if (_obj.ExchangeState == Docflow.OfficialDocument.ExchangeState.Obsolete)
          _obj.LifeCycleState = ContractBase.LifeCycleState.Obsolete;
        if (_obj.ExchangeState == Docflow.OfficialDocument.ExchangeState.Terminated)
          _obj.LifeCycleState = ContractBase.LifeCycleState.Terminated;
      }
    }

    public override void CounterpartyChanged(Sungero.Docflow.Shared.ContractualDocumentBaseCounterpartyChangedEventArgs e)
    {
      base.CounterpartyChanged(e);
      
      FillName();
    }

  }
}