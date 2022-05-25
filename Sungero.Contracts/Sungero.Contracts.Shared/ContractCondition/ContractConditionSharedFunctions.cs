using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ContractCondition;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;

namespace Sungero.Contracts.Shared
{
  partial class ContractConditionFunctions
  {
    /// <summary>
    /// Получить словарь поддерживаемых типов условий.
    /// </summary>
    /// <returns>
    /// Словарь.
    /// Ключ - GUID типа документа.
    /// Значение - список поддерживаемых условий.
    /// </returns>
    public override System.Collections.Generic.Dictionary<string, List<Enumeration?>> GetSupportedConditions()
    {
      var baseConditions = base.GetSupportedConditions();
      
      var contracts = Docflow.PublicFunctions.DocumentKind.GetDocumentGuids(typeof(IContractualDocumentBase));
      var accountings = Docflow.PublicFunctions.DocumentKind.GetDocumentGuids(typeof(IAccountingDocumentBase));
      var standards = Docflow.PublicFunctions.DocumentKind.GetDocumentGuids(typeof(IContractualDocument));
      
      // Сумма, валюта, нерезидент - для всех договорных(базовых) и финансовых типов.
      foreach (var typeGuid in contracts.Concat(accountings))
      {
        baseConditions[typeGuid].AddRange(new List<Enumeration?> { ConditionType.AmountIsMore,
                                            ConditionType.Currency,
                                            ConditionType.Nonresident });
      }
      
      // Условие типовой - только для договорных типов.
      foreach (var typeGuid in standards)
        baseConditions[typeGuid].Add(ConditionType.Standard);

      return baseConditions;
    }
    
    /// <summary>
    /// Проверить условие.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>True, если условие выполняется, и false - если не выполняется.</returns>
    public override Docflow.Structures.ConditionBase.ConditionResult CheckCondition(IOfficialDocument document, IApprovalTask task)
    {
      if (_obj.ConditionType == ConditionType.Standard)
        return this.CheckStandard(document, task);
      
      return base.CheckCondition(document, task);
    }
    
    /// <summary>
    /// Проверить условие "Типовой".
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>True, если документ типовой.</returns>
    private Docflow.Structures.ConditionBase.ConditionResult CheckStandard(IOfficialDocument document, IApprovalTask task)
    {
      if (Sungero.Contracts.ContractualDocuments.Is(document))
      {
        var contractualDocument = Sungero.Contracts.ContractualDocuments.As(document);
        
        if (!contractualDocument.IsStandard.HasValue)
          return Docflow.Structures.ConditionBase.ConditionResult.Create(null, ContractConditions.Resources.TheStandardIsNotFilledInContractCard);
        
        return Docflow.Structures.ConditionBase.ConditionResult.Create(contractualDocument.IsStandard.Value, string.Empty);
      }
      
      return Docflow.Structures.ConditionBase.ConditionResult.Create(null, ContractConditions.Resources.StandardFormPropertyCanBeCheckedOnlyForContracts);
    }
  }
}